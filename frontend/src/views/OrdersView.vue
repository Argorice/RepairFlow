<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
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
import PriorityMark from '@/components/orders/PriorityMark.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppPagination from '@/components/ui/AppPagination.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatDateTime, formatMoney } from '@/lib/format'
import { PRIORITY_LABELS, STATUS_LABELS } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()
const route = useRoute()
const router = useRouter()

const loading = ref(true)
const result = ref<PagedResult<OrderListItemDto> | null>(null)
const technicians = ref<UserDto[]>([])

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

const statusOptions = Object.entries(STATUS_LABELS).map(([value, label]) => ({ value, label }))
const priorityOptions = Object.entries(PRIORITY_LABELS).map(([value, label]) => ({ value, label }))
const sortOptions = [
  { value: '-createdAt', label: 'Сначала новые' },
  { value: 'createdAt', label: 'Сначала старые' },
  { value: '-updatedAt', label: 'Недавно изменённые' },
  { value: '-priority', label: 'Сначала срочные' },
  { value: 'number', label: 'По номеру' },
]

const technicianOptions = computed(() =>
  technicians.value.map((technician) => ({ value: technician.id, label: technician.fullName })),
)

const hasActiveFilters = computed(
  () =>
    Boolean(filters.search) ||
    Boolean(filters.status) ||
    Boolean(filters.priority) ||
    Boolean(filters.technicianId) ||
    Boolean(filters.from) ||
    Boolean(filters.to),
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

async function load(): Promise<void> {
  loading.value = true

  try {
    result.value = await ordersApi.list(buildQuery())
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить заявки.')
  } finally {
    loading.value = false
  }
}

let debounce: number | undefined

watch(
  () => ({ ...filters }),
  (next, previous) => {
    // Смена любого фильтра возвращает на первую страницу — иначе легко «провалиться» в пустоту.
    if (previous && next.page === previous.page && next !== previous) {
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
    debounce = window.setTimeout(load, 250)
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

onMounted(async () => {
  await load()

  if (auth.isManager) {
    try {
      technicians.value = await usersApi.list({ role: UserRole.Technician })
    } catch {
      // Справочник мастеров не критичен для списка — молча живём без фильтра по мастеру.
    }
  }
})
</script>

<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="font-display text-2xl font-bold">Заявки</h1>
        <p class="mt-1 text-sm text-slate-500">
          {{
            auth.isClient
              ? 'Ваши обращения в сервис'
              : auth.isTechnician
                ? 'Назначенные вам и свободные заявки'
                : 'Все заявки мастерской'
          }}
        </p>
      </div>

      <AppButton v-if="auth.isClient" @click="router.push({ name: 'order-create' })">
        Новая заявка
      </AppButton>
    </header>

    <!-- Фильтры -->
    <div class="card p-4">
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <AppInput v-model="filters.search" placeholder="Номер, модель, описание…" label="Поиск" />
        <AppSelect v-model="filters.status" label="Статус" placeholder="Любой" :options="statusOptions" />
        <AppSelect
          v-model="filters.priority"
          label="Приоритет"
          placeholder="Любой"
          :options="priorityOptions"
        />
        <AppSelect
          v-if="auth.isManager"
          v-model="filters.technicianId"
          label="Мастер"
          placeholder="Любой"
          :options="technicianOptions"
        />
        <AppInput v-model="filters.from" label="Создана с" type="date" />
        <AppInput v-model="filters.to" label="по" type="date" />
        <AppSelect v-model="filters.sort" label="Сортировка" :options="sortOptions" />

        <div class="flex items-end">
          <AppButton v-if="hasActiveFilters" variant="ghost" size="sm" @click="reset">
            Сбросить фильтры
          </AppButton>
        </div>
      </div>
    </div>

    <!-- Список -->
    <div class="card overflow-hidden">
      <SkeletonBlock v-if="loading" :lines="6" class="p-6" />

      <EmptyState
        v-else-if="!result || result.items.length === 0"
        title="Заявок не найдено"
        :description="
          hasActiveFilters
            ? 'Попробуйте ослабить фильтры — возможно, вы ищете слишком узко.'
            : auth.isClient
              ? 'Создайте первую заявку — это займёт минуту.'
              : 'Пока сюда ничего не поступало.'
        "
      >
        <AppButton v-if="auth.isClient && !hasActiveFilters" @click="router.push({ name: 'order-create' })">
          Создать заявку
        </AppButton>
        <AppButton v-else-if="hasActiveFilters" variant="secondary" @click="reset">
          Сбросить фильтры
        </AppButton>
      </EmptyState>

      <template v-else>
        <!-- Таблица на широком экране -->
        <table class="hidden w-full text-left text-sm md:table">
          <thead class="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th class="px-4 py-3 font-medium">Номер</th>
              <th class="px-4 py-3 font-medium">Устройство</th>
              <th class="px-4 py-3 font-medium">Статус</th>
              <th v-if="!auth.isClient" class="px-4 py-3 font-medium">Клиент</th>
              <th class="px-4 py-3 font-medium">Мастер</th>
              <th class="px-4 py-3 text-right font-medium">Смета</th>
              <th class="px-4 py-3 font-medium">Обновлена</th>
            </tr>
          </thead>

          <tbody class="divide-y divide-slate-100">
            <tr
              v-for="order in result.items"
              :key="order.id"
              class="cursor-pointer transition-colors hover:bg-slate-50"
              @click="router.push({ name: 'order-details', params: { id: order.id } })"
            >
              <td class="px-4 py-3">
                <span class="font-medium text-slate-900 tabular">{{ order.number }}</span>
                <PriorityMark :priority="order.priority" class="ml-2" />
              </td>
              <td class="px-4 py-3">
                <p class="text-slate-900">{{ order.brand }} {{ order.model }}</p>
                <p class="text-xs text-slate-500">{{ order.deviceType }}</p>
              </td>
              <td class="px-4 py-3">
                <StatusBadge :status="order.status" :label="order.statusLabel" />
              </td>
              <td v-if="!auth.isClient" class="px-4 py-3 text-slate-600">
                {{ order.client.fullName }}
              </td>
              <td class="px-4 py-3 text-slate-600">
                {{ order.technician?.fullName ?? '—' }}
              </td>
              <td class="px-4 py-3 text-right text-slate-900 tabular">
                {{ formatMoney(order.finalCost ?? order.estimatedCost) }}
              </td>
              <td class="px-4 py-3 text-slate-500 tabular">{{ formatDateTime(order.updatedAt) }}</td>
            </tr>
          </tbody>
        </table>

        <!-- Карточки на мобильном -->
        <ul class="divide-y divide-slate-100 md:hidden">
          <li v-for="order in result.items" :key="order.id">
            <RouterLink
              :to="{ name: 'order-details', params: { id: order.id } }"
              class="block px-4 py-4 transition-colors hover:bg-slate-50"
            >
              <div class="flex items-center justify-between gap-3">
                <span class="font-medium tabular">{{ order.number }}</span>
                <StatusBadge :status="order.status" :label="order.statusLabel" />
              </div>
              <p class="mt-1 text-sm text-slate-900">{{ order.brand }} {{ order.model }}</p>
              <div class="mt-2 flex items-center justify-between text-xs text-slate-500">
                <span>{{ order.technician?.fullName ?? 'Мастер не назначен' }}</span>
                <span class="tabular">{{ formatMoney(order.finalCost ?? order.estimatedCost) }}</span>
              </div>
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
  </div>
</template>
