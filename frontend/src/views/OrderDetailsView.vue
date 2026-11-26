<script setup lang="ts">
import { CircleAlert, Ellipsis, Radio } from '@lucide/vue'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/client'
import { estimateApi, ordersApi, usersApi } from '@/api/endpoints'
import {
  OrderEventKind,
  OrderStatus,
  UserRole,
  type OrderDetailsDto,
  type OrderEventDto,
  type OrderStatusHistoryDto,
  type UserDto,
} from '@/api/types'
import DeviceIcon from '@/components/orders/DeviceIcon.vue'
import OrderAttachments from '@/components/orders/OrderAttachments.vue'
import OrderComments from '@/components/orders/OrderComments.vue'
import OrderEstimate from '@/components/orders/OrderEstimate.vue'
import OrderHistory from '@/components/orders/OrderHistory.vue'
import PriorityMark from '@/components/orders/PriorityMark.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import StatusTimeline from '@/components/orders/StatusTimeline.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppSegmented from '@/components/ui/AppSegmented.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import AppTextarea from '@/components/ui/AppTextarea.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { appBar } from '@/lib/appBar'
import { formatDateTime, formatMoney } from '@/lib/format'
import { MAIN_PATH, isDestructiveTransition, statusLabel, transitionLabel } from '@/lib/status'
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
const notFound = ref(false)
const acting = ref(false)

type Tab = 'overview' | 'estimate' | 'comments' | 'files' | 'history'
const tab = ref<Tab>('overview')

const pendingStatus = ref<OrderStatus | null>(null)
const transitionComment = ref('')
const moreActionsOpen = ref(false)

const estimateRef = ref<InstanceType<typeof OrderEstimate> | null>(null)
const commentsRef = ref<InstanceType<typeof OrderComments> | null>(null)

const tabs = computed(() => [
  { value: 'overview' as const, label: t('details.tabs.overview') },
  { value: 'estimate' as const, label: t('details.tabs.estimate'), badge: order.value?.itemsCount || undefined },
  { value: 'comments' as const, label: t('details.tabs.comments'), badge: order.value?.commentsCount || undefined },
  { value: 'files' as const, label: t('details.tabs.files'), badge: order.value?.attachmentsCount || undefined },
  { value: 'history' as const, label: t('details.tabs.history') },
])

const technicianOptions = computed(() =>
  technicians.value.map((technician) => ({ value: technician.id, label: technician.fullName })),
)

/**
 * Кнопки переходов рисуются по списку, который отдал сервер: фронтенд не угадывает права.
 * Порядок — «вперёд по процессу», потом «шаг назад» (пересчёт сметы), потом отмена и отказ.
 * Основная кнопка одна: две одинаково яркие кнопки заставляют думать, какая из них главная.
 */
function rank(target: OrderStatus): number {
  if (isDestructiveTransition(target)) {
    return 2
  }

  const current = order.value ? MAIN_PATH.indexOf(order.value.status) : -1
  return MAIN_PATH.indexOf(target) < current ? 1 : 0
}

const transitions = computed(() =>
  [...(order.value?.availableTransitions ?? [])].sort((first, second) => rank(first.status) - rank(second.status)),
)

const primaryTransition = computed(() => {
  const first = transitions.value[0]
  return first && rank(first.status) === 0 ? first : null
})

const secondaryTransitions = computed(() =>
  transitions.value.filter((transition) => transition !== primaryTransition.value),
)

const price = computed(() => {
  if (!order.value) {
    return null
  }

  return order.value.finalCost ?? order.value.estimatedCost ?? (order.value.estimateTotal || null)
})

const awaitsClientDecision = computed(
  () => auth.isClient && order.value?.status === OrderStatus.AwaitingEstimateApproval,
)

async function load(silent = false): Promise<void> {
  if (!silent) {
    loading.value = true
  }

  try {
    const [details, entries] = await Promise.all([ordersApi.byId(orderId.value), ordersApi.history(orderId.value)])

    order.value = details
    history.value = entries
    notFound.value = false
    appBar.title = details.number
  } catch (error) {
    if (error instanceof ApiError && (error.status === 404 || error.status === 403)) {
      notFound.value = true
    } else {
      toasts.error(error instanceof ApiError ? error.message : t('details.loadFailed'))
    }
  } finally {
    loading.value = false
  }
}

function askForStatus(status: OrderStatus): void {
  moreActionsOpen.value = false
  pendingStatus.value = status
  transitionComment.value = ''
}

async function applyStatus(): Promise<void> {
  if (!pendingStatus.value || !order.value) {
    return
  }

  acting.value = true
  const target = pendingStatus.value

  try {
    const comment = transitionComment.value.trim() || undefined

    // Решение клиента по смете идёт через её собственные эндпоинты: сервер сам пишет в историю,
    // что смету подтвердил или отклонил именно клиент.
    if (auth.isClient && order.value.status === OrderStatus.AwaitingEstimateApproval && target === OrderStatus.InProgress) {
      order.value = await estimateApi.approve(orderId.value)
    } else if (
      auth.isClient &&
      order.value.status === OrderStatus.AwaitingEstimateApproval &&
      target === OrderStatus.ClientRejected
    ) {
      order.value = await estimateApi.reject(orderId.value, comment)
    } else {
      order.value = await ordersApi.changeStatus(orderId.value, target, comment)
    }

    history.value = await ordersApi.history(orderId.value)
    estimateRef.value?.reload()
    toasts.success(t('details.statusChanged', { status: statusLabel(target) }))
    pendingStatus.value = null
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('details.statusFailed'))
  } finally {
    acting.value = false
  }
}

async function assign(technicianId: string): Promise<void> {
  acting.value = true

  try {
    order.value = await ordersApi.assign(orderId.value, technicianId || null)
    history.value = await ordersApi.history(orderId.value)
    toasts.success(technicianId ? t('details.assigned') : t('details.unassigned'))
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('details.assignFailed'))
  } finally {
    acting.value = false
  }
}

/**
 * Живые обновления: пришло событие — тихо перезагружаем то, что изменилось.
 * Текст уведомления собирается здесь, на языке этого пользователя, а не берётся из события:
 * событие сформировал тот, кто сделал изменение, и он мог говорить на другом языке.
 */
function describeEvent(event: OrderEventDto): string {
  switch (event.kind) {
    case OrderEventKind.CommentAdded:
      return t('events.comment', { number: event.number })
    case OrderEventKind.TechnicianAssigned:
      return t('events.assigned', { number: event.number })
    default:
      return t('events.status', { number: event.number, status: statusLabel(event.status) })
  }
}

const { connected } = useOrderEvents(orderId, (event: OrderEventDto) => {
  void load(true)

  if (event.kind === OrderEventKind.CommentAdded) {
    commentsRef.value?.reload()
  } else {
    estimateRef.value?.reload()
  }

  toasts.info(describeEvent(event))
})

watch(orderId, () => {
  tab.value = 'overview'
  void load()
})

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

onBeforeUnmount(() => {
  appBar.title = ''
})
</script>

<template>
  <div class="space-y-4 lg:space-y-6" :class="transitions.length ? 'pb-24 lg:pb-0' : ''">
    <div v-if="loading" class="space-y-4">
      <div class="card p-5"><SkeletonBlock :lines="4" /></div>
      <div class="card p-5"><SkeletonBlock :lines="6" /></div>
    </div>

    <div v-else-if="notFound" class="card">
      <EmptyState :icon="CircleAlert" :title="t('details.notFoundTitle')" :description="t('details.notFoundText')">
        <AppButton variant="secondary" @click="$router.push({ name: 'orders' })">{{ t('notFound.back') }}</AppButton>
      </EmptyState>
    </div>

    <div v-else-if="order" class="lg:grid lg:grid-cols-[minmax(0,1fr)_320px] lg:items-start lg:gap-6">
      <div class="min-w-0 space-y-4 lg:space-y-6">
        <!-- Шапка: что за устройство, где оно сейчас и сколько стоит -->
        <header class="card p-4 sm:p-6">
          <div class="flex items-start gap-3 sm:gap-4">
            <DeviceIcon :type="order.deviceType" />

            <div class="min-w-0 flex-1">
              <div class="flex flex-wrap items-center gap-x-2.5 gap-y-1">
                <h1 class="font-display text-xl leading-tight font-bold sm:text-2xl">
                  {{ order.brand }} {{ order.model }}
                </h1>
                <PriorityMark :priority="order.priority" with-label />
              </div>
              <p class="mt-1 text-sm text-ink-3">
                {{ order.number }} · {{ order.deviceType }}
                <template v-if="order.serialNumber"> · S/N {{ order.serialNumber }}</template>
              </p>

              <div class="mt-3 flex flex-wrap items-center gap-2">
                <StatusBadge :status="order.status" />
                <span
                  v-if="connected"
                  class="inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-xs text-success-ink"
                  :title="t('details.liveHint')"
                >
                  <Radio class="size-3.5" aria-hidden="true" />
                  {{ t('details.live') }}
                </span>
              </div>
            </div>

            <div class="hidden shrink-0 text-right sm:block lg:hidden">
              <p class="text-xs text-ink-3">{{ order.finalCost !== null ? t('details.total') : t('details.preliminary') }}</p>
              <p class="font-display text-2xl font-bold tabular">{{ formatMoney(price) }}</p>
            </div>
          </div>

          <div class="mt-5 border-t border-line pt-5">
            <StatusTimeline :status="order.status" :history="history" />
          </div>

          <!-- Клиенту, от которого ждут решения, — прямой путь к смете -->
          <button
            v-if="awaitsClientDecision"
            type="button"
            class="pressable mt-5 flex w-full items-center gap-3 rounded-2xl bg-warn-soft px-4 py-3 text-left ring-1 ring-warn-line ring-inset"
            @click="tab = 'estimate'"
          >
            <CircleAlert class="size-5 shrink-0 text-warn-ink" aria-hidden="true" />
            <span class="flex-1 text-sm font-medium text-warn-ink">{{ t('details.decisionCallout') }}</span>
            <span class="text-sm font-semibold text-ink tabular">{{ formatMoney(order.estimateTotal) }}</span>
          </button>
        </header>

        <!-- Вкладки: на телефоне прилипают под верхней панелью -->
        <div class="sticky top-[calc(3.5rem+env(safe-area-inset-top))] z-20 -mx-4 bg-canvas/90 px-4 py-2 backdrop-blur-lg sm:mx-0 sm:px-0 lg:static lg:bg-transparent lg:p-0 lg:backdrop-blur-none">
          <AppSegmented v-model="tab" :options="tabs" :label="t('details.tabsLabel')" />
        </div>

        <section class="card p-4 sm:p-6">
          <div v-if="tab === 'overview'" class="space-y-6">
            <div>
              <h2 class="section-title">{{ t('details.problem') }}</h2>
              <p class="mt-2 text-[15px] leading-relaxed whitespace-pre-line text-ink-2 sm:text-sm">
                {{ order.problemDescription }}
              </p>
            </div>

            <!-- На широком экране эти сведения живут в правой колонке -->
            <dl class="grid grid-cols-2 gap-x-4 gap-y-4 border-t border-line pt-5 text-sm lg:hidden">
              <div>
                <dt class="text-ink-3">{{ t('details.client') }}</dt>
                <dd class="mt-0.5 font-medium text-ink">{{ order.client.fullName }}</dd>
              </div>
              <div>
                <dt class="text-ink-3">{{ t('details.technician') }}</dt>
                <dd class="mt-0.5 font-medium text-ink">{{ order.technician?.fullName ?? t('details.unassignedShort') }}</dd>
              </div>
              <div>
                <dt class="text-ink-3">{{ t('details.created') }}</dt>
                <dd class="mt-0.5 text-ink tabular">{{ formatDateTime(order.createdAt) }}</dd>
              </div>
              <div>
                <dt class="text-ink-3">{{ order.finalCost !== null ? t('details.total') : t('details.preliminary') }}</dt>
                <dd class="mt-0.5 font-semibold text-ink tabular">{{ formatMoney(price) }}</dd>
              </div>
            </dl>

            <AppSelect
              v-if="auth.isManager"
              class="lg:hidden"
              :model-value="order.technician?.id ?? ''"
              :label="t('details.assign')"
              :placeholder="t('details.unassignedShort')"
              :options="technicianOptions"
              :disabled="acting"
              @update:model-value="assign"
            />
          </div>

          <OrderEstimate
            v-else-if="tab === 'estimate'"
            ref="estimateRef"
            :order-id="order.id"
            @changed="load(true)"
          />

          <OrderComments v-else-if="tab === 'comments'" ref="commentsRef" :order-id="order.id" />

          <OrderAttachments v-else-if="tab === 'files'" :order-id="order.id" :can-upload="order.canEdit || auth.isStaff" />

          <OrderHistory v-else :history="history" />
        </section>
      </div>

      <!-- Правая колонка на широком экране: деньги, люди, действия -->
      <aside class="hidden space-y-4 lg:sticky lg:top-8 lg:block">
        <div class="card p-5">
          <p class="text-sm text-ink-3">{{ order.finalCost !== null ? t('details.total') : t('details.preliminary') }}</p>
          <p class="mt-1 font-display text-3xl font-bold tabular">{{ formatMoney(price) }}</p>

          <div v-if="transitions.length" class="mt-5 grid gap-2">
            <AppButton
              v-for="transition in transitions"
              :key="transition.status"
              :variant="transition === primaryTransition ? 'primary' : 'secondary'"
              :disabled="acting"
              block
              @click="askForStatus(transition.status)"
            >
              {{ transitionLabel(order.status, transition.status) }}
            </AppButton>
          </div>
        </div>

        <dl class="card divide-y divide-line text-sm">
          <div class="px-5 py-3.5">
            <dt class="text-ink-3">{{ t('details.client') }}</dt>
            <dd class="mt-0.5 font-medium text-ink">{{ order.client.fullName }}</dd>
            <dd class="text-xs text-ink-3">{{ order.client.email }}</dd>
          </div>
          <div class="px-5 py-3.5">
            <dt class="text-ink-3">{{ t('details.technician') }}</dt>
            <dd v-if="!auth.isManager" class="mt-0.5 font-medium text-ink">
              {{ order.technician?.fullName ?? t('details.unassignedShort') }}
            </dd>
            <dd v-else class="mt-1.5">
              <AppSelect
                :model-value="order.technician?.id ?? ''"
                :aria-label="t('details.assign')"
                :placeholder="t('details.unassignedShort')"
                :options="technicianOptions"
                :disabled="acting"
                @update:model-value="assign"
              />
            </dd>
          </div>
          <div class="px-5 py-3.5">
            <dt class="text-ink-3">{{ t('details.created') }}</dt>
            <dd class="mt-0.5 text-ink tabular">{{ formatDateTime(order.createdAt) }}</dd>
          </div>
          <div class="px-5 py-3.5">
            <dt class="text-ink-3">{{ t('details.updated') }}</dt>
            <dd class="mt-0.5 text-ink tabular">{{ formatDateTime(order.updatedAt) }}</dd>
          </div>
        </dl>
      </aside>
    </div>

    <!-- Панель действий на телефоне: под большим пальцем, поверх контента -->
    <Teleport to="body">
      <div
        v-if="order && transitions.length"
        class="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-[var(--rf-glass)] px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] backdrop-blur-xl backdrop-saturate-150 lg:hidden"
      >
        <div class="mx-auto flex max-w-xl gap-2">
          <AppButton
            v-if="primaryTransition"
            size="lg"
            class="flex-1"
            :disabled="acting"
            @click="askForStatus(primaryTransition.status)"
          >
            {{ transitionLabel(order.status, primaryTransition.status) }}
          </AppButton>

          <!-- Одно второстепенное действие помещается рядом, несколько — уходят в шторку -->
          <AppButton
            v-if="secondaryTransitions.length === 1 && secondaryTransitions[0]"
            size="lg"
            variant="secondary"
            :class="primaryTransition ? '' : 'flex-1'"
            :disabled="acting"
            @click="askForStatus(secondaryTransitions[0].status)"
          >
            {{ transitionLabel(order.status, secondaryTransitions[0].status) }}
          </AppButton>
          <AppButton
            v-else-if="secondaryTransitions.length > 1"
            size="lg"
            variant="secondary"
            square
            :icon="Ellipsis"
            :label="t('details.moreActions')"
            @click="moreActionsOpen = true"
          />
        </div>
      </div>
    </Teleport>

    <AppModal v-if="moreActionsOpen && order" :title="t('details.moreActions')" @close="moreActionsOpen = false">
      <div class="grid gap-2">
        <AppButton
          v-for="transition in secondaryTransitions"
          :key="transition.status"
          size="lg"
          :variant="isDestructiveTransition(transition.status) ? 'danger' : 'secondary'"
          block
          @click="askForStatus(transition.status)"
        >
          {{ transitionLabel(order.status, transition.status) }}
        </AppButton>
      </div>
    </AppModal>

    <!-- Подтверждение перехода: комментарий попадёт в историю -->
    <AppModal
      v-if="pendingStatus && order"
      :title="transitionLabel(order.status, pendingStatus)"
      :description="t('details.confirmText', { status: statusLabel(pendingStatus) })"
      @close="pendingStatus = null"
    >
      <AppTextarea
        v-model="transitionComment"
        :label="t('details.comment')"
        :rows="3"
        :placeholder="t('common.optional')"
      />

      <template #actions>
        <AppButton variant="secondary" @click="pendingStatus = null">{{ t('common.cancel') }}</AppButton>
        <AppButton
          :variant="isDestructiveTransition(pendingStatus) ? 'danger' : 'primary'"
          :loading="acting"
          @click="applyStatus"
        >
          {{ t('common.confirm') }}
        </AppButton>
      </template>
    </AppModal>
  </div>
</template>
