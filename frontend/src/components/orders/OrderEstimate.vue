<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { estimateApi } from '@/api/endpoints'
import { OrderItemType, type EstimateDto, type OrderItemDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatMoney } from '@/lib/format'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const props = defineProps<{ orderId: string }>()
const emit = defineEmits<{ changed: [] }>()

const auth = useAuthStore()
const toasts = useToastStore()

const estimate = ref<EstimateDto | null>(null)
const loading = ref(true)
const saving = ref(false)
const editingId = ref<string | null>(null)

const typeOptions = [
  { value: OrderItemType.Labor, label: 'Работа' },
  { value: OrderItemType.Part, label: 'Запчасть' },
]

const draft = ref({
  type: OrderItemType.Labor as OrderItemType,
  name: '',
  quantity: '1',
  unitPrice: '',
})

async function load(): Promise<void> {
  loading.value = true

  try {
    estimate.value = await estimateApi.get(props.orderId)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить смету.')
  } finally {
    loading.value = false
  }
}

function resetDraft(): void {
  draft.value = { type: OrderItemType.Labor, name: '', quantity: '1', unitPrice: '' }
  editingId.value = null
}

function startEdit(item: OrderItemDto): void {
  editingId.value = item.id
  draft.value = {
    type: item.type,
    name: item.name,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
  }
}

async function save(): Promise<void> {
  saving.value = true

  const payload = {
    type: draft.value.type,
    name: draft.value.name,
    quantity: Number(draft.value.quantity.replace(',', '.')),
    unitPrice: Number(draft.value.unitPrice.replace(',', '.')),
  }

  try {
    if (editingId.value) {
      await estimateApi.update(props.orderId, editingId.value, payload)
    } else {
      await estimateApi.add(props.orderId, payload)
    }

    resetDraft()
    await load()
    emit('changed')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось сохранить позицию.')
  } finally {
    saving.value = false
  }
}

async function remove(item: OrderItemDto): Promise<void> {
  try {
    await estimateApi.remove(props.orderId, item.id)
    await load()
    emit('changed')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось удалить позицию.')
  }
}

async function approve(): Promise<void> {
  saving.value = true

  try {
    await estimateApi.approve(props.orderId)
    toasts.success('Смета подтверждена, заявка в работе')
    await load()
    emit('changed')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось подтвердить смету.')
  } finally {
    saving.value = false
  }
}

async function reject(): Promise<void> {
  saving.value = true

  try {
    await estimateApi.reject(props.orderId)
    toasts.info('Вы отказались от ремонта')
    await load()
    emit('changed')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось отклонить смету.')
  } finally {
    saving.value = false
  }
}

defineExpose({ reload: load })

onMounted(load)
</script>

<template>
  <div class="space-y-6">
    <SkeletonBlock v-if="loading" :lines="4" />

    <template v-else-if="estimate">
      <!-- Решение клиента по смете — самое важное действие на этой вкладке -->
      <div
        v-if="estimate.awaitingApproval && auth.isClient"
        class="rounded-xl border border-amber-200 bg-amber-50 p-4"
      >
        <p class="font-medium text-amber-900">Смета готова и ждёт вашего решения</p>
        <p class="mt-1 text-sm text-amber-800">
          Итого к оплате {{ formatMoney(estimate.total, true) }}. Подтвердите — и мастер начнёт ремонт.
        </p>

        <div class="mt-4 flex flex-wrap gap-3">
          <AppButton :loading="saving" @click="approve">Подтвердить смету</AppButton>
          <AppButton variant="secondary" :disabled="saving" @click="reject">Отказаться от ремонта</AppButton>
        </div>
      </div>

      <div
        v-else-if="estimate.awaitingApproval"
        class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
      >
        Смета отправлена клиенту и ждёт подтверждения.
      </div>

      <EmptyState
        v-if="estimate.items.length === 0"
        title="Смета пустая"
        :description="
          estimate.isEditable
            ? 'Добавьте работы и запчасти — после этого заявку можно отправить на согласование.'
            : 'Мастер ещё не добавил ни одной позиции.'
        "
      />

      <div v-else class="overflow-hidden rounded-xl border border-slate-200">
        <table class="w-full text-left text-sm">
          <thead class="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-2 font-medium">Позиция</th>
              <th class="px-4 py-2 font-medium">Тип</th>
              <th class="px-4 py-2 text-right font-medium">Кол-во</th>
              <th class="px-4 py-2 text-right font-medium">Цена</th>
              <th class="px-4 py-2 text-right font-medium">Сумма</th>
              <th v-if="estimate.isEditable" class="px-4 py-2" />
            </tr>
          </thead>

          <tbody class="divide-y divide-slate-100">
            <tr v-for="item in estimate.items" :key="item.id">
              <td class="px-4 py-2 text-slate-900">{{ item.name }}</td>
              <td class="px-4 py-2 text-slate-500">{{ item.typeLabel }}</td>
              <td class="px-4 py-2 text-right tabular">{{ item.quantity }}</td>
              <td class="px-4 py-2 text-right tabular">{{ formatMoney(item.unitPrice, true) }}</td>
              <td class="px-4 py-2 text-right font-medium tabular">{{ formatMoney(item.total, true) }}</td>
              <td v-if="estimate.isEditable" class="px-4 py-2 text-right whitespace-nowrap">
                <button class="text-xs text-slate-500 hover:text-brand-600" @click="startEdit(item)">
                  Изменить
                </button>
                <button class="ml-3 text-xs text-slate-500 hover:text-rose-600" @click="remove(item)">
                  Удалить
                </button>
              </td>
            </tr>
          </tbody>

          <tfoot class="bg-slate-50 text-sm">
            <tr>
              <td colspan="4" class="px-4 py-2 text-right text-slate-500">Работы</td>
              <td class="px-4 py-2 text-right tabular">{{ formatMoney(estimate.laborTotal, true) }}</td>
              <td v-if="estimate.isEditable" />
            </tr>
            <tr>
              <td colspan="4" class="px-4 py-2 text-right text-slate-500">Запчасти</td>
              <td class="px-4 py-2 text-right tabular">{{ formatMoney(estimate.partsTotal, true) }}</td>
              <td v-if="estimate.isEditable" />
            </tr>
            <tr class="border-t border-slate-200">
              <td colspan="4" class="px-4 py-3 text-right font-medium text-slate-900">Итого</td>
              <td class="px-4 py-3 text-right font-display text-base font-bold tabular">
                {{ formatMoney(estimate.total, true) }}
              </td>
              <td v-if="estimate.isEditable" />
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Форма позиции -->
      <form v-if="estimate.isEditable" class="rounded-xl border border-slate-200 p-4" @submit.prevent="save">
        <p class="mb-3 font-medium text-slate-900">
          {{ editingId ? 'Изменить позицию' : 'Добавить позицию' }}
        </p>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <AppSelect v-model="draft.type" label="Тип" :options="typeOptions" />
          <AppInput v-model="draft.name" label="Название" placeholder="Замена дисплея" required />
          <AppInput v-model="draft.quantity" label="Количество" placeholder="1" required />
          <AppInput v-model="draft.unitPrice" label="Цена за единицу" placeholder="2200" required />
        </div>

        <div class="mt-4 flex justify-end gap-3">
          <AppButton v-if="editingId" variant="ghost" @click="resetDraft">Отмена</AppButton>
          <AppButton type="submit" :loading="saving">{{ editingId ? 'Сохранить' : 'Добавить' }}</AppButton>
        </div>
      </form>
    </template>
  </div>
</template>
