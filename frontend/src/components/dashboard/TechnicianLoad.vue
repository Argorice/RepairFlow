<script setup lang="ts">
import { computed } from 'vue'
import type { TechnicianLoadDto } from '@/api/types'
import EmptyState from '@/components/ui/EmptyState.vue'
import { formatMoney } from '@/lib/format'

const props = defineProps<{ items: TechnicianLoadDto[] }>()

/**
 * Одна серия — один цвет на все полосы. Красить «больше — темнее» было бы двойным
 * кодированием: длина полосы и так показывает величину.
 */
const BAR_COLOR = '#4F46E5'

const max = computed(() => Math.max(1, ...props.items.map((item) => item.activeOrders)))
</script>

<template>
  <section class="card p-5">
    <header class="mb-4">
      <h2 class="font-display text-base font-semibold">Загрузка мастеров</h2>
      <p class="mt-0.5 text-sm text-slate-500">Активные заявки и выручка за период</p>
    </header>

    <EmptyState v-if="items.length === 0" title="Мастеров нет" description="Заведите сотрудников в разделе «Пользователи»." />

    <ul v-else class="space-y-4">
      <li v-for="item in items" :key="item.technicianId">
        <div class="flex items-baseline justify-between gap-3 text-sm">
          <span class="truncate font-medium text-slate-900">{{ item.fullName }}</span>
          <span class="shrink-0 text-xs text-slate-500 tabular">
            {{ item.completedInPeriod }} выдано · {{ formatMoney(item.revenueInPeriod) }}
          </span>
        </div>

        <div class="mt-2 flex items-center gap-3">
          <div class="h-3 flex-1 overflow-hidden rounded-r bg-slate-100">
            <div
              class="h-full rounded-r transition-[width] duration-500"
              :style="{
                width: `${Math.max((item.activeOrders / max) * 100, item.activeOrders > 0 ? 4 : 0)}%`,
                backgroundColor: BAR_COLOR,
              }"
            />
          </div>

          <!-- Значение у конца полосы: подпись, а не число на каждом пикселе -->
          <span class="w-10 shrink-0 text-right text-sm font-medium text-slate-900 tabular">
            {{ item.activeOrders }}
          </span>
        </div>
      </li>
    </ul>
  </section>
</template>
