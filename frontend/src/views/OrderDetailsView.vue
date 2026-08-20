<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/client'
import { ordersApi, usersApi } from '@/api/endpoints'
import {
  OrderEventKind,
  UserRole,
  type OrderDetailsDto,
  type OrderEventDto,
  type OrderStatus,
  type OrderStatusHistoryDto,
  type UserDto,
} from '@/api/types'
import OrderAttachments from '@/components/orders/OrderAttachments.vue'
import OrderComments from '@/components/orders/OrderComments.vue'
import OrderEstimate from '@/components/orders/OrderEstimate.vue'
import OrderHistory from '@/components/orders/OrderHistory.vue'
import PriorityMark from '@/components/orders/PriorityMark.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import StatusTimeline from '@/components/orders/StatusTimeline.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import AppTextarea from '@/components/ui/AppTextarea.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatDateTime, formatMoney } from '@/lib/format'
import { useOrderEvents } from '@/realtime/orderEvents'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const route = useRoute()
const auth = useAuthStore()
const toasts = useToastStore()

const orderId = computed(() => String(route.params.id))

const order = ref<OrderDetailsDto | null>(null)
const history = ref<OrderStatusHistoryDto[]>([])
const technicians = ref<UserDto[]>([])

const loading = ref(true)
const acting = ref(false)

type Tab = 'description' | 'estimate' | 'comments' | 'files' | 'history'
const tab = ref<Tab>('description')

const pendingStatus = ref<{ status: OrderStatus; label: string } | null>(null)
const transitionComment = ref('')

const estimateRef = ref<InstanceType<typeof OrderEstimate> | null>(null)
const commentsRef = ref<InstanceType<typeof OrderComments> | null>(null)

const tabs = computed(() => [
  { key: 'description' as const, label: 'Описание' },
  { key: 'estimate' as const, label: `Смета${order.value?.itemsCount ? ` · ${order.value.itemsCount}` : ''}` },
  {
    key: 'comments' as const,
    label: `Комментарии${order.value?.commentsCount ? ` · ${order.value.commentsCount}` : ''}`,
  },
  { key: 'files' as const, label: `Файлы${order.value?.attachmentsCount ? ` · ${order.value.attachmentsCount}` : ''}` },
  { key: 'history' as const, label: 'История' },
])

const technicianOptions = computed(() => [
  ...technicians.value.map((technician) => ({ value: technician.id, label: technician.fullName })),
])

async function load(silent = false): Promise<void> {
  if (!silent) {
    loading.value = true
  }

  try {
    const [details, entries] = await Promise.all([
      ordersApi.byId(orderId.value),
      ordersApi.history(orderId.value),
    ])

    order.value = details
    history.value = entries
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить заявку.')
  } finally {
    loading.value = false
  }
}

function askForStatus(status: OrderStatus, label: string): void {
  pendingStatus.value = { status, label }
  transitionComment.value = ''
}

async function applyStatus(): Promise<void> {
  if (!pendingStatus.value) {
    return
  }

  acting.value = true

  try {
    order.value = await ordersApi.changeStatus(
      orderId.value,
      pendingStatus.value.status,
      transitionComment.value || undefined,
    )

    history.value = await ordersApi.history(orderId.value)
    toasts.success(`Статус изменён: ${pendingStatus.value.label}`)
    pendingStatus.value = null
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось сменить статус.')
  } finally {
    acting.value = false
  }
}

async function assign(technicianId: string): Promise<void> {
  acting.value = true

  try {
    order.value = await ordersApi.assign(orderId.value, technicianId || null)
    toasts.success(technicianId ? 'Мастер назначен' : 'Мастер снят с заявки')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось назначить мастера.')
  } finally {
    acting.value = false
  }
}

/** Живые обновления: пришло событие — тихо перезагружаем то, что изменилось. */
const { connected } = useOrderEvents(orderId, (event: OrderEventDto) => {
  void load(true)

  if (event.kind === OrderEventKind.CommentAdded) {
    commentsRef.value?.reload()
  } else {
    estimateRef.value?.reload()
  }

  toasts.info(event.message ?? `Заявка ${event.number}: ${event.statusLabel}`)
})

watch(orderId, () => void load())

onMounted(async () => {
  await load()

  if (auth.isManager) {
    try {
      technicians.value = await usersApi.list({ role: UserRole.Technician })
    } catch {
      // Без списка мастеров карточка всё равно работает, просто без назначения.
    }
  }
})
</script>

<template>
  <div class="space-y-6">
    <SkeletonBlock v-if="loading" :lines="8" />

    <template v-else-if="order">
      <!-- Шапка -->
      <header class="card p-6">
        <div class="flex flex-wrap items-start justify-between gap-4">
          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-3">
              <h1 class="font-display text-2xl font-bold tabular">{{ order.number }}</h1>
              <StatusBadge :status="order.status" :label="order.statusLabel" />
              <PriorityMark :priority="order.priority" />
              <span
                v-if="connected"
                class="inline-flex items-center gap-1.5 text-xs text-slate-400"
                title="Изменения приходят в реальном времени"
              >
                <span class="size-1.5 rounded-full bg-emerald-500" />
                онлайн
              </span>
            </div>

            <p class="mt-2 text-slate-900">{{ order.brand }} {{ order.model }}</p>
            <p class="text-sm text-slate-500">
              {{ order.deviceType }}
              <template v-if="order.serialNumber"> · S/N {{ order.serialNumber }}</template>
            </p>
          </div>

          <div class="text-right">
            <p class="text-sm text-slate-500">
              {{ order.finalCost !== null ? 'Итого' : 'Предварительно' }}
            </p>
            <p class="font-display text-2xl font-bold tabular">
              {{ formatMoney(order.finalCost ?? order.estimatedCost ?? order.estimateTotal) }}
            </p>
          </div>
        </div>

        <!-- Кнопки переходов рисуются по списку, который отдал сервер -->
        <div v-if="order.availableTransitions.length" class="mt-6 flex flex-wrap gap-3">
          <AppButton
            v-for="transition in order.availableTransitions"
            :key="transition.status"
            :variant="transition.status === 'Cancelled' || transition.status === 'ClientRejected' ? 'secondary' : 'primary'"
            :disabled="acting"
            @click="askForStatus(transition.status, transition.label)"
          >
            {{ transition.label }}
          </AppButton>
        </div>

        <div class="mt-6 border-t border-slate-100 pt-6">
          <StatusTimeline :status="order.status" :history="history" />
        </div>
      </header>

      <!-- Вкладки -->
      <div class="card overflow-hidden">
        <nav class="flex gap-1 overflow-x-auto border-b border-slate-200 px-2" role="tablist">
          <button
            v-for="item in tabs"
            :key="item.key"
            role="tab"
            :aria-selected="tab === item.key"
            class="whitespace-nowrap border-b-2 px-3 py-3 text-sm font-medium transition-colors"
            :class="
              tab === item.key
                ? 'border-brand-600 text-brand-700'
                : 'border-transparent text-slate-500 hover:text-slate-900'
            "
            @click="tab = item.key"
          >
            {{ item.label }}
          </button>
        </nav>

        <div class="p-6">
          <!-- Описание -->
          <div v-if="tab === 'description'" class="space-y-6">
            <div>
              <h2 class="font-display text-base font-semibold">Что случилось</h2>
              <p class="mt-2 whitespace-pre-line text-sm text-slate-700">{{ order.problemDescription }}</p>
            </div>

            <dl class="grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
              <div>
                <dt class="text-slate-500">Клиент</dt>
                <dd class="mt-0.5 text-slate-900">{{ order.client.fullName }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">Мастер</dt>
                <dd class="mt-0.5 text-slate-900">{{ order.technician?.fullName ?? 'Не назначен' }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">Создана</dt>
                <dd class="mt-0.5 text-slate-900 tabular">{{ formatDateTime(order.createdAt) }}</dd>
              </div>
              <div>
                <dt class="text-slate-500">Обновлена</dt>
                <dd class="mt-0.5 text-slate-900 tabular">{{ formatDateTime(order.updatedAt) }}</dd>
              </div>
            </dl>

            <div v-if="auth.isManager" class="max-w-xs">
              <AppSelect
                :model-value="order.technician?.id ?? ''"
                label="Назначить мастера"
                placeholder="Не назначен"
                :options="technicianOptions"
                :disabled="acting"
                @update:model-value="assign"
              />
            </div>
          </div>

          <OrderEstimate
            v-else-if="tab === 'estimate'"
            ref="estimateRef"
            :order-id="order.id"
            @changed="load(true)"
          />

          <OrderComments v-else-if="tab === 'comments'" ref="commentsRef" :order-id="order.id" />

          <OrderAttachments
            v-else-if="tab === 'files'"
            :order-id="order.id"
            :can-upload="order.canEdit || auth.isStaff"
          />

          <OrderHistory v-else :history="history" />
        </div>
      </div>
    </template>

    <!-- Подтверждение перехода: комментарий попадёт в историю -->
    <AppModal
      v-if="pendingStatus"
      :title="pendingStatus.label"
      description="Комментарий попадёт в историю заявки. Его видят клиент и сотрудники."
      @close="pendingStatus = null"
    >
      <AppTextarea v-model="transitionComment" label="Комментарий" :rows="3" placeholder="Необязательно" />

      <template #actions>
        <AppButton variant="secondary" @click="pendingStatus = null">Отмена</AppButton>
        <AppButton :loading="acting" @click="applyStatus">Подтвердить</AppButton>
      </template>
    </AppModal>
  </div>
</template>
