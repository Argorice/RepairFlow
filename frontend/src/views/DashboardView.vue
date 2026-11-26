<script setup lang="ts">
import { CircleAlert, ClipboardList, PackageCheck, Timer, Wallet } from '@lucide/vue'
import { computed, onMounted, ref, watch } from 'vue'
import { ApiError } from '@/api/client'
import { dashboardApi } from '@/api/endpoints'
import type { DashboardSummaryDto } from '@/api/types'
import OrdersChart from '@/components/dashboard/OrdersChart.vue'
import StatTile from '@/components/dashboard/StatTile.vue'
import TechnicianLoad from '@/components/dashboard/TechnicianLoad.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import StatusBadge from '@/components/orders/StatusBadge.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppSegmented from '@/components/ui/AppSegmented.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatHours, formatMoney, toDateInput } from '@/lib/format'
import { useToastStore } from '@/stores/toasts'

const toasts = useToastStore()

const summary = ref<DashboardSummaryDto | null>(null)
const loading = ref(true)
const refreshing = ref(false)

/**
 * Период — переключатель, а не две даты: «за неделю» и «за месяц» спрашивают в сто раз чаще,
 * чем «с 3 по 17 число». Произвольный период остаётся за последним вариантом.
 */
type Period = '7' | '30' | '90' | 'custom'
const period = ref<Period>('30')
const custom = ref({ from: '', to: '' })

const periodOptions = computed(() => [
  { value: '7' as const, label: t('dashboard.periods.week') },
  { value: '30' as const, label: t('dashboard.periods.month') },
  { value: '90' as const, label: t('dashboard.periods.quarter') },
  { value: 'custom' as const, label: t('dashboard.periods.custom') },
])

const range = computed(() => {
  if (period.value === 'custom') {
    return { from: custom.value.from || undefined, to: custom.value.to || undefined }
  }

  const to = new Date()
  const from = new Date()
  from.setDate(to.getDate() - Number(period.value) + 1)

  return { from: toDateInput(from), to: toDateInput(to) }
})

async function load(): Promise<void> {
  // При смене периода не показываем скелетон заново — придерживаем прошлую отрисовку.
  if (summary.value) {
    refreshing.value = true
  }

  try {
    summary.value = await dashboardApi.summary(range.value)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('dashboard.loadFailed'))
  } finally {
    loading.value = false
    refreshing.value = false
  }
}

const statuses = computed(() => summary.value?.byStatus.filter((item) => item.count > 0) ?? [])

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
  <div class="space-y-5 lg:space-y-6">
    <PageHeader :title="t('nav.dashboard')" :description="t('dashboard.subtitle')">
      <!-- Один ряд фильтров над всем, что он охватывает, — не по фильтру в каждой карточке -->
      <template #actions>
        <AppSegmented v-model="period" :options="periodOptions" :label="t('dashboard.period')" />
      </template>
    </PageHeader>

    <div v-if="period === 'custom'" class="grid max-w-md animate-fade-in grid-cols-2 gap-3">
      <AppInput v-model="custom.from" :label="t('dashboard.from')" type="date" />
      <AppInput v-model="custom.to" :label="t('dashboard.to')" type="date" />
    </div>

    <template v-if="loading">
      <SkeletonBlock variant="cards" :lines="4" />
      <div class="card p-5"><SkeletonBlock :lines="6" /></div>
    </template>

    <div v-else-if="summary" class="space-y-5 transition-opacity lg:space-y-6" :class="refreshing ? 'opacity-60' : ''">
      <div class="grid grid-cols-2 gap-3 sm:gap-4 xl:grid-cols-4">
        <StatTile
          :icon="ClipboardList"
          :label="t('dashboard.open')"
          :value="String(summary.openOrders)"
          :hint="t('dashboard.totalOrders', { count: summary.totalOrders })"
        />
        <StatTile :icon="PackageCheck" :label="t('dashboard.completed')" :value="String(summary.completedInPeriod)" />
        <StatTile :icon="Wallet" :label="t('dashboard.revenue')" :value="formatMoney(summary.revenueInPeriod)" />
        <StatTile
          :icon="Timer"
          :label="t('dashboard.avgRepair')"
          :value="formatHours(summary.averageRepairHours)"
          :hint="t('dashboard.avgRepairHint')"
        />
      </div>

      <RouterLink
        v-if="summary.overdueEstimates > 0"
        :to="{ name: 'orders', query: { status: 'AwaitingEstimateApproval' } }"
        class="pressable flex items-center gap-3 rounded-2xl bg-warn-soft p-4 ring-1 ring-warn-line ring-inset sm:p-5"
      >
        <CircleAlert class="size-5 shrink-0 text-warn-ink" aria-hidden="true" />
        <div class="min-w-0 flex-1">
          <p class="font-medium text-warn-ink">
            {{ t('dashboard.overdue', { count: summary.overdueEstimates }) }}
          </p>
          <p class="mt-0.5 text-sm text-warn-ink/85">{{ t('dashboard.overdueHint') }}</p>
        </div>
        <span class="hidden shrink-0 text-sm font-medium text-warn-ink underline underline-offset-2 sm:inline">
          {{ t('dashboard.showOrders') }}
        </span>
      </RouterLink>

      <div class="grid gap-5 lg:grid-cols-3 lg:gap-6">
        <div class="min-w-0 lg:col-span-2">
          <OrdersChart :daily="summary.daily" />
        </div>

        <TechnicianLoad :items="summary.technicianLoad" />
      </div>

      <section class="card p-4 sm:p-5">
        <h2 class="section-title">{{ t('dashboard.byStatus') }}</h2>

        <ul class="mt-4 flex flex-wrap gap-2 sm:gap-3">
          <li v-for="item in statuses" :key="item.status">
            <RouterLink
              :to="{ name: 'orders', query: { status: item.status } }"
              class="pressable flex items-center justify-between gap-3 rounded-xl px-3 py-2.5 ring-1 ring-line transition-colors ring-inset hover:bg-surface-2"
            >
              <StatusBadge :status="item.status" />
              <span class="font-semibold text-ink tabular">{{ item.count }}</span>
            </RouterLink>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>
