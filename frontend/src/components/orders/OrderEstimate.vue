<script setup lang="ts">
import { Package, Pencil, Plus, Receipt, Trash2, Wrench } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { estimateApi } from '@/api/endpoints'
import { OrderItemType, type EstimateDto, type OrderItemDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppSegmented from '@/components/ui/AppSegmented.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatMoney, formatNumber } from '@/lib/format'
import { itemTypeLabel } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const props = defineProps<{ orderId: string }>()
const emit = defineEmits<{ changed: [] }>()

const auth = useAuthStore()
const toasts = useToastStore()

const estimate = ref<EstimateDto | null>(null)
const loading = ref(true)
const saving = ref(false)

/** Форма позиции открывается шторкой: null — закрыта, 'new' — новая позиция, иначе id правки. */
const editing = ref<'new' | string | null>(null)
const formError = ref<ApiError | null>(null)

const typeOptions = computed(() => [
  { value: OrderItemType.Labor, label: itemTypeLabel(OrderItemType.Labor), icon: Wrench },
  { value: OrderItemType.Part, label: itemTypeLabel(OrderItemType.Part), icon: Package },
])

const draft = ref({
  type: OrderItemType.Labor as OrderItemType,
  name: '',
  quantity: '1',
  unitPrice: '',
})

async function load(): Promise<void> {
  try {
    estimate.value = await estimateApi.get(props.orderId)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('estimate.loadFailed'))
  } finally {
    loading.value = false
  }
}

function openNew(): void {
  draft.value = { type: OrderItemType.Labor, name: '', quantity: '1', unitPrice: '' }
  formError.value = null
  editing.value = 'new'
}

function openEdit(item: OrderItemDto): void {
  draft.value = {
    type: item.type,
    name: item.name,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
  }
  formError.value = null
  editing.value = item.id
}

/** Запятая и точка — обе допустимы: в русской раскладке десятичный разделитель — запятая. */
const toNumber = (value: string): number => Number(value.replace(',', '.').replace(/\s/g, ''))

async function save(): Promise<void> {
  saving.value = true
  formError.value = null

  const payload = {
    type: draft.value.type,
    name: draft.value.name,
    quantity: toNumber(draft.value.quantity),
    unitPrice: toNumber(draft.value.unitPrice),
  }

  try {
    if (editing.value && editing.value !== 'new') {
      await estimateApi.update(props.orderId, editing.value, payload)
    } else {
      await estimateApi.add(props.orderId, payload)
    }

    editing.value = null
    await load()
    emit('changed')
  } catch (error) {
    formError.value = error instanceof ApiError ? error : null
    toasts.error(formError.value?.message ?? t('estimate.saveFailed'))
  } finally {
    saving.value = false
  }
}

const editingItem = computed(() =>
  editing.value && editing.value !== 'new'
    ? (estimate.value?.items.find((item) => item.id === editing.value) ?? null)
    : null,
)

async function removeEditing(): Promise<void> {
  const item = editingItem.value
  editing.value = null

  if (item) {
    await remove(item)
  }
}

async function remove(item: OrderItemDto): Promise<void> {
  try {
    await estimateApi.remove(props.orderId, item.id)
    toasts.info(t('estimate.removed', { name: item.name }))
    await load()
    emit('changed')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('estimate.removeFailed'))
  }
}

defineExpose({ reload: load })

onMounted(load)
</script>

<template>
  <div class="space-y-5">
    <SkeletonBlock v-if="loading" :lines="4" />

    <template v-else-if="estimate">
      <!--
        Решение клиента по смете. Кнопки «подтвердить» и «отказаться» живут в панели действий
        карточки — одна пара на экран, а не две одинаковые в разных местах.
      -->
      <div
        v-if="estimate.awaitingApproval && auth.isClient"
        class="rounded-2xl bg-warn-soft p-4 ring-1 ring-warn-line ring-inset sm:p-5"
      >
        <p class="font-semibold text-warn-ink">{{ t('estimate.decisionTitle') }}</p>
        <p class="mt-1 text-sm text-warn-ink/90">{{ t('estimate.decisionText') }}</p>
        <p class="mt-3 font-display text-3xl font-bold text-ink tabular">
          {{ formatMoney(estimate.total, true) }}
        </p>
      </div>

      <p
        v-else-if="estimate.awaitingApproval"
        class="rounded-2xl bg-warn-soft px-4 py-3 text-sm text-warn-ink ring-1 ring-warn-line ring-inset"
      >
        {{ t('estimate.sentToClient') }}
      </p>

      <EmptyState
        v-if="estimate.items.length === 0"
        :icon="Receipt"
        :title="t('estimate.emptyTitle')"
        :description="estimate.isEditable ? t('estimate.emptyEditable') : t('estimate.emptyReadonly')"
        compact
      >
        <AppButton v-if="estimate.isEditable" variant="soft" :icon="Plus" @click="openNew">
          {{ t('estimate.addItem') }}
        </AppButton>
      </EmptyState>

      <template v-else>
        <!-- Строка сметы целиком — цель касания: открывает правку, а удаление живёт внутри шторки -->
        <ul class="divide-y divide-line overflow-hidden rounded-2xl ring-1 ring-line ring-inset">
          <li
            v-for="item in estimate.items"
            :key="item.id"
            class="flex items-center gap-3 px-4 py-3"
            :class="estimate.isEditable ? 'cursor-pointer transition-colors hover:bg-surface-2 active:bg-surface-2' : ''"
            @click="estimate.isEditable && openEdit(item)"
          >
            <span
              class="flex size-9 shrink-0 items-center justify-center rounded-xl"
              :class="item.type === OrderItemType.Part ? 'bg-info-soft text-info-ink' : 'bg-accent-soft text-accent-ink'"
              :title="itemTypeLabel(item.type)"
            >
              <Package v-if="item.type === OrderItemType.Part" class="size-[18px]" aria-hidden="true" />
              <Wrench v-else class="size-[18px]" aria-hidden="true" />
            </span>

            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium text-ink">{{ item.name }}</p>
              <p class="truncate text-xs text-ink-3 tabular">
                {{ itemTypeLabel(item.type) }} · {{ formatNumber(item.quantity) }} × {{ formatMoney(item.unitPrice, true) }}
              </p>
            </div>

            <p class="shrink-0 text-sm font-semibold text-ink tabular">{{ formatMoney(item.total, true) }}</p>

            <div v-if="estimate.isEditable" class="-mr-2 hidden shrink-0 sm:flex" @click.stop>
              <AppButton
                variant="ghost"
                size="sm"
                square
                :icon="Pencil"
                :label="t('common.edit')"
                @click="openEdit(item)"
              />
              <AppButton
                variant="ghost"
                size="sm"
                square
                :icon="Trash2"
                :label="t('common.delete')"
                @click="remove(item)"
              />
            </div>
          </li>
        </ul>

        <dl class="space-y-2 rounded-2xl bg-surface-2 px-4 py-4 text-sm">
          <div class="flex justify-between">
            <dt class="text-ink-2">{{ t('estimate.labor') }}</dt>
            <dd class="text-ink tabular">{{ formatMoney(estimate.laborTotal, true) }}</dd>
          </div>
          <div class="flex justify-between">
            <dt class="text-ink-2">{{ t('estimate.parts') }}</dt>
            <dd class="text-ink tabular">{{ formatMoney(estimate.partsTotal, true) }}</dd>
          </div>
          <div class="flex items-baseline justify-between border-t border-line pt-3">
            <dt class="font-medium text-ink">{{ t('estimate.total') }}</dt>
            <dd class="font-display text-xl font-bold text-ink tabular">{{ formatMoney(estimate.total, true) }}</dd>
          </div>
        </dl>

        <AppButton v-if="estimate.isEditable" variant="soft" :icon="Plus" block @click="openNew">
          {{ t('estimate.addItem') }}
        </AppButton>
      </template>
    </template>

    <AppModal
      v-if="editing"
      :title="editing === 'new' ? t('estimate.addItem') : t('estimate.editItem')"
      @close="editing = null"
    >
      <form id="estimate-item" class="space-y-4" @submit.prevent="save">
        <AppSegmented v-model="draft.type" :options="typeOptions" :label="t('estimate.type')" block />

        <AppInput
          v-model="draft.name"
          :label="t('estimate.name')"
          :placeholder="draft.type === OrderItemType.Part ? t('estimate.namePartHint') : t('estimate.nameLaborHint')"
          required
          :error="formError?.fieldError('name')"
        />

        <div class="grid grid-cols-2 gap-3">
          <AppInput
            v-model="draft.quantity"
            :label="t('estimate.quantity')"
            inputmode="decimal"
            required
            :error="formError?.fieldError('quantity')"
          />
          <AppInput
            v-model="draft.unitPrice"
            :label="t('estimate.unitPrice')"
            inputmode="decimal"
            placeholder="60"
            required
            :error="formError?.fieldError('unitPrice')"
          />
        </div>
      </form>

      <template #actions>
        <AppButton v-if="editingItem" variant="danger" :icon="Trash2" class="sm:mr-auto" @click="removeEditing">
          {{ t('common.delete') }}
        </AppButton>
        <AppButton variant="secondary" @click="editing = null">{{ t('common.cancel') }}</AppButton>
        <AppButton type="submit" form="estimate-item" :loading="saving">
          {{ editing === 'new' ? t('common.add') : t('common.save') }}
        </AppButton>
      </template>
    </AppModal>
  </div>
</template>
