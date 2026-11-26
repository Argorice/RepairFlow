<script setup lang="ts">
import { ArrowDown, Flame } from '@lucide/vue'
import { OrderPriority } from '@/api/types'
import { t } from '@/i18n'
import { priorityLabel } from '@/lib/status'

/** Обычный приоритет не рисуется вовсе: метка есть только у того, что отличается от нормы. */
defineProps<{ priority: OrderPriority; withLabel?: boolean }>()
</script>

<template>
  <span
    v-if="priority !== OrderPriority.Normal"
    class="inline-flex items-center gap-1 text-xs font-medium"
    :class="priority === OrderPriority.High ? 'text-danger-ink' : 'text-ink-3'"
    :title="t('priority.tooltip', { value: priorityLabel(priority) })"
  >
    <Flame v-if="priority === OrderPriority.High" class="size-3.5" aria-hidden="true" />
    <ArrowDown v-else class="size-3.5" aria-hidden="true" />
    <span :class="withLabel ? '' : 'sr-only'">{{ priorityLabel(priority) }}</span>
  </span>
</template>
