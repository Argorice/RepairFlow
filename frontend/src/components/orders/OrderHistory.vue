<script setup lang="ts">
import type { OrderStatusHistoryDto } from '@/api/types'
import EmptyState from '@/components/ui/EmptyState.vue'
import { formatDateTime } from '@/lib/format'
import { statusStyle } from '@/lib/status'

defineProps<{ history: OrderStatusHistoryDto[] }>()
</script>

<template>
  <EmptyState v-if="history.length === 0" title="История пуста" />

  <ol v-else class="space-y-5">
    <li v-for="entry in history" :key="entry.id" class="relative flex gap-4 pl-1">
      <span class="mt-1 size-3 shrink-0 rounded-full" :class="statusStyle(entry.toStatus).solid" />

      <div class="min-w-0 flex-1 border-b border-slate-100 pb-4">
        <p class="text-sm text-slate-900">
          <template v-if="entry.fromStatusLabel">
            {{ entry.fromStatusLabel }} → <strong class="font-medium">{{ entry.toStatusLabel }}</strong>
          </template>
          <template v-else>
            <strong class="font-medium">{{ entry.toStatusLabel }}</strong>
          </template>
        </p>

        <p v-if="entry.comment" class="mt-1 text-sm text-slate-600">{{ entry.comment }}</p>

        <p class="mt-1 text-xs text-slate-400 tabular">
          {{ entry.changedBy.fullName }} · {{ formatDateTime(entry.changedAt) }}
        </p>
      </div>
    </li>
  </ol>
</template>
