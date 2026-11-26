<script setup lang="ts">
import { Users } from '@lucide/vue'
import { computed } from 'vue'
import type { TechnicianLoadDto } from '@/api/types'
import AppAvatar from '@/components/ui/AppAvatar.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import { t } from '@/i18n'
import { formatMoney } from '@/lib/format'

const props = defineProps<{ items: TechnicianLoadDto[] }>()

/**
 * Одна серия — один цвет на все полосы. Красить «больше — темнее» было бы двойным
 * кодированием: длина полосы и так показывает величину.
 */
const max = computed(() => Math.max(1, ...props.items.map((item) => item.activeOrders)))
</script>

<template>
  <section class="card p-4 sm:p-5">
    <header class="mb-5">
      <h2 class="section-title">{{ t('dashboard.loadTitle') }}</h2>
      <p class="mt-0.5 text-sm text-ink-2">{{ t('dashboard.loadSubtitle') }}</p>
    </header>

    <EmptyState
      v-if="items.length === 0"
      :icon="Users"
      :title="t('dashboard.noTechnicians')"
      :description="t('dashboard.noTechniciansHint')"
      compact
    />

    <ul v-else class="space-y-5">
      <li v-for="item in items" :key="item.technicianId" class="flex items-center gap-3">
        <AppAvatar :name="item.fullName" size="sm" tone="neutral" />

        <div class="min-w-0 flex-1">
          <div class="flex items-baseline justify-between gap-3 text-sm">
            <span class="truncate font-medium text-ink">{{ item.fullName }}</span>
            <span class="shrink-0 text-xs text-ink-3 tabular">
              {{ t('dashboard.handedOver', { count: item.completedInPeriod }) }} ·
              {{ formatMoney(item.revenueInPeriod) }}
            </span>
          </div>

          <div class="mt-2 flex items-center gap-3">
            <div class="h-2 flex-1 overflow-hidden rounded-full bg-surface-2">
              <div
                class="h-full rounded-full bg-[var(--rf-chart-1)] transition-[width] duration-500 ease-[var(--ease-app)]"
                :style="{ width: `${Math.max((item.activeOrders / max) * 100, item.activeOrders > 0 ? 4 : 0)}%` }"
              />
            </div>

            <!-- Значение у конца полосы: подпись, а не число на каждом пикселе -->
            <span
              class="w-8 shrink-0 text-right text-sm font-semibold text-ink tabular"
              :title="t('dashboard.activeOrders', { count: item.activeOrders })"
            >
              {{ item.activeOrders }}
            </span>
          </div>
        </div>
      </li>
    </ul>
  </section>
</template>
