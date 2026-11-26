<script setup lang="ts">
import { History } from '@lucide/vue'
import type { OrderStatusHistoryDto } from '@/api/types'
import EmptyState from '@/components/ui/EmptyState.vue'
import { t } from '@/i18n'
import { formatDateTime } from '@/lib/format'
import { statusLabel, statusStyle } from '@/lib/status'

defineProps<{ history: OrderStatusHistoryDto[] }>()
</script>

<template>
  <EmptyState v-if="history.length === 0" :icon="History" :title="t('history.empty')" compact />

  <!-- Лента: вертикальная линия связывает события, точка окрашена в цвет нового статуса -->
  <ol v-else class="relative">
    <li v-for="(entry, index) in history" :key="entry.id" class="relative flex gap-4 pb-6 last:pb-0">
      <span
        v-if="index < history.length - 1"
        class="absolute top-5 bottom-0 left-[7px] w-px bg-line"
        aria-hidden="true"
      />
      <span
        class="relative mt-1.5 size-[15px] shrink-0 rounded-full ring-4 ring-surface"
        :class="statusStyle(entry.toStatus).solid"
        aria-hidden="true"
      />

      <div class="min-w-0 flex-1">
        <p class="text-sm text-ink">
          <template v-if="entry.fromStatus">
            <span class="text-ink-2">{{ statusLabel(entry.fromStatus) }}</span>
            <span class="mx-1.5 text-ink-3" aria-hidden="true">→</span>
            <span class="sr-only">{{ t('history.to') }}</span>
            <strong class="font-semibold">{{ statusLabel(entry.toStatus) }}</strong>
          </template>
          <strong v-else class="font-semibold">{{ t('history.created') }}</strong>
        </p>

        <p v-if="entry.comment" class="mt-1.5 rounded-xl bg-surface-2 px-3 py-2 text-sm text-ink-2">
          {{ entry.comment }}
        </p>

        <p class="mt-1.5 text-xs text-ink-3 tabular">
          {{ entry.changedBy.fullName }} · {{ formatDateTime(entry.changedAt) }}
        </p>
      </div>
    </li>
  </ol>
</template>
