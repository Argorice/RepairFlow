<script setup lang="ts">
import { ChevronRight, ClipboardList, Plus, Search, SlidersHorizontal } from '@lucide/vue'
import { computed, onActivated, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/client'
import { ordersApi, usersApi } from '@/api/endpoints'
import {
  OrderPriority,
  OrderStatus,
  UserRole,
  type OrderListItemDto,
  type OrderQuery,
  type PagedResult,
  type UserDto,
} from '@/api/types'
import PageHeader from '@/components/layout/PageHeader.vue'
import DeviceIcon from '@/components/orders/DeviceIcon.vue'
import PriorityMark from '@/components/orders/PriorityMark.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppChip from '@/components/ui/AppChip.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatMoney, formatRelative } from '@/lib/format'
import { deviceTypeLabel } from '@/lib/devices'
import { ALL_STATUSES, priorityLabel, statusLabel } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()
const route = useRoute()
const router = useRouter()

const loading = ref(true)
const result = ref<PagedResult<OrderListItemDto> | null>(null)
const technicians = ref<UserDto[]>([])
const filtersOpen = ref(false)

/** Фильтры живут в query-строке: ссылку на отобранный список можно просто переслать коллеге. */
const filters = reactive({
  search: (route.query.search as string) ?? '',
  status: (route.query.status as string) ?? '',
  priority: (route.query.priority as string) ?? '',
  technicianId: (route.query.technicianId as string) ?? '',
  from: (route.query.from as string) ?? '',
  to: (route.query.to as string) ?? '',
  sort: (route.query.sort as string) ?? '-createdAt',
  page: Number(route.query.page ?? 1),
})

const priorityOptions = computed(() =>
  Object.values(OrderPriority).map((value) => ({ value, label: priorityLabel(value) })),
)

const sortOptions = computed(() => [
  { value: '-createdAt', label: t('orders.sort.newest') },
  { value: 'createdAt', label: t('orders.sort.oldest') },
  { value: '-updatedAt', label: t('orders.sort.updated') },
  { value: '-priority', label: t('orders.sort.urgent') },
  { value: 'number', label: t('orders.sort.number') },
])

const technicianOptions = computed(() =>
  technicians.value.map((technician) => ({ value: technician.id, label: technician.fullName })),
)

/** Сколько фильтров спрятано в шторке — число на кнопке, чтобы активный фильтр не терялся. */
const hiddenFilterCount = computed(
  () => [filters.priority, filters.technicianId, filters.from, filters.to].filter(Boolean).length,
)

const hasActiveFilters = computed(
  () => Boolean(filters.search) || Boolean(filters.status) || hiddenFilterCount.value > 0,
)

const subtitle = computed(() =>
  auth.isClient
    ? t('orders.subtitleClient')
    : auth.isTechnician
      ? t('orders.subtitleTechnician')
      : t('orders.subtitleManager'),
)

function buildQuery(): OrderQuery {
  return {
    search: filters.search || undefined,
    status: (filters.status as OrderStatus) || undefined,
    priority: (filters.priority as OrderPriority) || undefined,
    technicianId: filters.technicianId || undefined,
    from: filters.from || undefined,
    to: filters.to || undefined,
    sort: filters.sort,
    page: filters.page,
    pageSize: 20,
  }
}

async function load(silent = false): Promise<void> {
  if (!silent) {
    loading.value = true
  }

  try {
    result.value = await ordersApi.list(buildQuery())
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('orders.loadFailed'))
  } finally {
    loading.value = false
  }
}

let debounce: number | undefined

watch(
  () => ({ ...filters }),
  (next, previous) => {
    // Смена любого фильтра возвращает на первую страницу — иначе легко «провалиться» в пустоту.
    if (previous && next.page === previous.page) {
      const changedFilter = Object.keys(next).some(
        (key) => key !== 'page' && next[key as keyof typeof next] !== previous[key as keyof typeof previous],
      )

      if (changedFilter && filters.page !== 1) {
        filters.page = 1
        return
      }
    }

    const query: Record<string, string> = {}
    for (const [key, value] of Object.entries(next)) {
      if (value !== '' && value !== undefined && !(key === 'page' && value === 1)) {
        query[key] = String(value)
      }
    }

    void router.replace({ query })

    window.clearTimeout(debounce)
    debounce = window.setTimeout(() => void load(), 250)
  },
  { deep: true },
)

function reset(): void {
  filters.search = ''
  filters.status = ''
  filters.priority = ''
  filters.technicianId = ''
  filters.from = ''
  filters.to = ''
}

function open(order: OrderListItemDto): void {
  void router.push({ name: 'order-details', params: { id: order.id } })
}

let mounted = false

onMounted(async () => {
  await load()
  mounted = true

  if (auth.isManager) {
    try {
      technicians.value = await usersApi.list({ role: UserRole.Technician })
    } catch {
      // Справочник мастеров не критичен для списка — молча живём без фильтра по мастеру.
    }
  }
})

// Список держится в памяти, пока открыта карточка. Вернулись — тихо подтягиваем изменения.
onActivated(() => {
  if (mounted) {
    void load(true)
  }
})
</script>

<template>
  <div class="space-y-5">
    <!-- Кнопка «Новая заявка» живёт в меню: в боковом на широком экране, в нижнем на телефоне -->
    <PageHeader :title="auth.isClient ? t('nav.myOrders') : t('nav.orders')" :description="subtitle" />

    <!-- Поиск и фильтры -->
    <div class="space-y-3">
      <div class="flex gap-2">
        <AppInput
          v-model="filters.search"
          class="flex-1"
          type="search"
          inputmode="search"
          :icon="Search"
          :placeholder="t('orders.searchPlaceholder')"
          :aria-label="t('orders.search')"
        />

        <AppSelect
          v-model="filters.sort"
          class="hidden w-56 sm:block"
          :options="sortOptions"
          :aria-label="t('orders.sortLabel')"
        />

        <button
          type="button"
          class="pressable relative inline-flex h-11 shrink-0 items-center gap-2 rounded-xl bg-surface px-3.5 text-sm font-medium text-ink ring-1 ring-line ring-inset hover:bg-surface-2 sm:h-10"
          :aria-label="t('orders.filters')"
          @click="filtersOpen = true"
        >
          <SlidersHorizontal class="size-[18px]" aria-hidden="true" />
          <span class="hidden sm:inline">{{ t('orders.filters') }}</span>
          <span
            v-if="hiddenFilterCount"
            class="absolute -top-1.5 -right-1.5 flex size-5 items-center justify-center rounded-full bg-accent text-[11px] font-semibold text-on-accent sm:static sm:size-auto sm:min-w-5 sm:px-1.5"
          >
            {{ hiddenFilterCount }}
          </span>
        </button>
      </div>

      <!-- Быстрый фильтр по статусу: самый частый вопрос — «что сейчас в работе» -->
      <div
        class="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 scrollbar-none sm:mx-0 sm:flex-wrap sm:px-0"
        role="group"
        :aria-label="t('orders.statusFilter')"
      >
        <AppChip :active="!filters.status" @click="filters.status = ''">{{ t('orders.allStatuses') }}</AppChip>
        <AppChip
          v-for="status in ALL_STATUSES"
          :key="status"
          :active="filters.status === status"
          @click="filters.status = filters.status === status ? '' : status"
        >
          {{ statusLabel(status) }}
        </AppChip>
      </div>
    </div>

    <!-- Список -->
    <div class="card overflow-hidden">
      <SkeletonBlock v-if="loading" variant="rows" :lines="6" />

      <EmptyState
        v-else-if="!result || result.items.length === 0"
        :icon="ClipboardList"
        :title="t('orders.emptyTitle')"
        :description="
          hasActiveFilters
            ? t('orders.emptyFiltered')
            : auth.isClient
              ? t('orders.emptyClient')
              : t('orders.emptyStaff')
        "
      >
        <AppButton
          v-if="auth.isClient && !hasActiveFilters"
          :icon="Plus"
          @click="router.push({ name: 'order-create' })"
        >
          {{ t('nav.newOrder') }}
        </AppButton>
        <AppButton v-else-if="hasActiveFilters" variant="secondary" @click="reset">
          {{ t('orders.resetFilters') }}
        </AppButton>
      </EmptyState>

      <template v-else>
        <!-- Таблица на широком экране -->
        <table class="hidden w-full text-left text-sm md:table">
          <thead class="border-b border-line text-xs text-ink-3">
            <tr>
              <th class="py-3 pr-4 pl-5 font-medium">{{ t('orders.columns.request') }}</th>
              <th class="px-4 py-3 font-medium">{{ t('orders.columns.status') }}</th>
              <th v-if="!auth.isClient" class="px-4 py-3 font-medium">{{ t('orders.columns.client') }}</th>
              <th class="px-4 py-3 font-medium">{{ t('orders.columns.technician') }}</th>
              <th class="px-4 py-3 text-right font-medium">{{ t('orders.columns.estimate') }}</th>
              <th class="py-3 pr-5 pl-4 text-right font-medium">{{ t('orders.columns.updated') }}</th>
            </tr>
          </thead>

          <tbody class="divide-y divide-line">
            <tr
              v-for="order in result.items"
              :key="order.id"
              class="cursor-pointer transition-colors hover:bg-surface-2"
              @click="open(order)"
            >
              <td class="py-3 pr-4 pl-5">
                <RouterLink
                  :to="{ name: 'order-details', params: { id: order.id } }"
                  class="flex items-center gap-3 rounded-lg"
                  @click.stop
                >
                  <DeviceIcon :type="order.deviceType" size="sm" />
                  <span class="min-w-0">
                    <span class="flex items-center gap-2">
                      <span class="truncate font-medium text-ink">{{ order.brand }} {{ order.model }}</span>
                      <PriorityMark :priority="order.priority" />
                    </span>
                    <span class="block text-xs text-ink-3">{{ order.number }} · {{ deviceTypeLabel(order.deviceType) }}</span>
                  </span>
                </RouterLink>
              </td>
              <td class="px-4 py-3">
                <StatusBadge :status="order.status" />
              </td>
              <td v-if="!auth.isClient" class="px-4 py-3 text-ink-2">{{ order.client.fullName }}</td>
              <td class="px-4 py-3 text-ink-2">
                {{ order.technician?.fullName ?? '—' }}
              </td>
              <td class="px-4 py-3 text-right text-ink tabular">
                {{ formatMoney(order.finalCost ?? order.estimatedCost) }}
              </td>
              <td class="py-3 pr-5 pl-4 text-right whitespace-nowrap text-ink-3">{{ formatRelative(order.updatedAt) }}</td>
            </tr>
          </tbody>
        </table>

        <!-- Список на телефоне: строка как в нативном приложении, вся строка — одна цель касания -->
        <ul class="divide-y divide-line md:hidden">
          <li v-for="order in result.items" :key="order.id">
            <RouterLink
              :to="{ name: 'order-details', params: { id: order.id } }"
              class="flex items-center gap-3 px-4 py-3.5 transition-colors active:bg-surface-2"
            >
              <DeviceIcon :type="order.deviceType" />

              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2">
                  <p class="truncate font-medium text-ink">{{ order.brand }} {{ order.model }}</p>
                  <PriorityMark :priority="order.priority" />
                </div>
                <p class="mt-0.5 truncate text-xs text-ink-3">
                  {{ order.number }} · {{ formatRelative(order.updatedAt) }}
                </p>
                <div class="mt-2 flex items-center justify-between gap-2">
                  <StatusBadge :status="order.status" size="sm" />
                  <span class="text-sm font-medium text-ink tabular">
                    {{ formatMoney(order.finalCost ?? order.estimatedCost) }}
                  </span>
                </div>
              </div>

              <ChevronRight class="size-5 shrink-0 text-ink-3" aria-hidden="true" />
            </RouterLink>
          </li>
        </ul>

        <AppPagination
          :page="result.page"
          :total-pages="result.totalPages"
          :total-count="result.totalCount"
          :has-previous="result.hasPrevious"
          :has-next="result.hasNext"
          @change="(page) => (filters.page = page)"
        />
      </template>
    </div>

    <!-- Остальные фильтры — в шторке: на телефоне им не место в основном экране -->
    <AppModal v-if="filtersOpen" :title="t('orders.filters')" @close="filtersOpen = false">
      <div class="space-y-4">
        <AppSelect
          v-model="filters.sort"
          class="sm:hidden"
          :label="t('orders.sortLabel')"
          :options="sortOptions"
        />
        <AppSelect
          v-model="filters.priority"
          :label="t('orders.priority')"
          :placeholder="t('orders.any')"
          :options="priorityOptions"
        />
        <AppSelect
          v-if="auth.isManager"
          v-model="filters.technicianId"
          :label="t('orders.columns.technician')"
          :placeholder="t('orders.any')"
          :options="technicianOptions"
        />
        <div class="grid grid-cols-2 gap-3">
          <AppInput v-model="filters.from" :label="t('orders.createdFrom')" type="date" />
          <AppInput v-model="filters.to" :label="t('orders.createdTo')" type="date" />
        </div>
      </div>

      <template #actions>
        <AppButton variant="secondary" :disabled="!hasActiveFilters" @click="reset">
          {{ t('orders.resetFilters') }}
        </AppButton>
        <AppButton @click="filtersOpen = false">{{ t('orders.showResults') }}</AppButton>
      </template>
    </AppModal>
  </div>
</template>
