<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ApiError } from '@/api/client'
import { dashboardApi } from '@/api/endpoints'
import type { DashboardSummaryDto } from '@/api/types'
import OrdersChart from '@/components/dashboard/OrdersChart.vue'
import StatTile from '@/components/dashboard/StatTile.vue'
import TechnicianLoad from '@/components/dashboard/TechnicianLoad.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatHours, formatMoney } from '@/lib/format'
import { useToastStore } from '@/stores/toasts'

const toasts = useToastStore()

const summary = ref<DashboardSummaryDto | null>(null)
const loading = ref(true)
const refreshing = ref(false)

const range = ref({ from: '', to: '' })

async function load(): Promise<void> {
  // При смене периода не показываем скелетон заново — придерживаем прошлую отрисовку.
  if (summary.value) {
    refreshing.value = true
  }

  try {
    summary.value = await dashboardApi.summary({
      from: range.value.from || undefined,
      to: range.value.to || undefined,
    })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить сводку.')
  } finally {
    loading.value = false
    refreshing.value = false
  }
}

const openStatuses = computed(() => summary.value?.byStatus.filter((item) => item.count > 0) ?? [])

let debounce: number | undefined

watch(
  range,
  () => {
    window.clearTimeout(debounce)
    debounce = window.setTimeout(load, 300)
  },
  { deep: true },
)

onMounted(load)
</script>

<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="font-display text-2xl font-bold">Дашборд</h1>
        <p class="mt-1 text-sm text-slate-500">Что происходит в мастерской прямо сейчас</p>
      </div>

      <!-- Один ряд фильтров над всем, что он охватывает, — не по фильтру в каждой карточке -->
      <div class="flex gap-3">
        <AppInput v-model="range.from" label="Период с" type="date" />
        <AppInput v-model="range.to" label="по" type="date" />
      </div>
    </header>

    <SkeletonBlock v-if="loading" :lines="8" />

    <div v-else-if="summary" class="space-y-6 transition-opacity" :class="refreshing ? 'opacity-60' : ''">
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="В работе сейчас" :value="String(summary.openOrders)" :hint="`всего заявок: ${summary.totalOrders}`" />
        <StatTile label="Выдано за период" :value="String(summary.completedInPeriod)" />
        <StatTile label="Выручка за период" :value="formatMoney(summary.revenueInPeriod)" />
        <StatTile
          label="Средний срок ремонта"
          :value="formatHours(summary.averageRepairHours)"
          hint="от создания до выдачи"
        />
      </div>

      <div
        v-if="summary.overdueEstimates > 0"
        class="card flex flex-wrap items-center justify-between gap-3 border-amber-200 bg-amber-50/60 p-5"
      >
        <div>
          <p class="font-medium text-amber-900">
            {{ summary.overdueEstimates }} смет ждут ответа клиента дольше трёх дней
          </p>
          <p class="mt-0.5 text-sm text-amber-800">Стоит позвонить — заявки стоят на месте.</p>
        </div>

        <RouterLink
          :to="{ name: 'orders', query: { status: 'AwaitingEstimateApproval' } }"
          class="text-sm font-medium text-amber-900 underline underline-offset-2"
        >
          Показать заявки
        </RouterLink>
      </div>

      <div class="grid gap-6 lg:grid-cols-3">
        <div class="lg:col-span-2">
          <OrdersChart :daily="summary.daily" />
        </div>

        <TechnicianLoad :items="summary.technicianLoad" />
      </div>

      <section class="card p-5">
        <h2 class="font-display text-base font-semibold">Заявки по статусам</h2>

        <ul class="mt-4 flex flex-wrap gap-3">
          <li v-for="item in openStatuses" :key="item.status">
            <RouterLink
              :to="{ name: 'orders', query: { status: item.status } }"
              class="flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 transition-colors hover:bg-slate-50"
            >
              <StatusBadge :status="item.status" :label="item.label" />
              <span class="font-medium tabular">{{ item.count }}</span>
            </RouterLink>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>
