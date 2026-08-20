<script setup lang="ts">
import { OrderPriority } from '@/api/types'
import { PRIORITY_LABELS, PRIORITY_STYLES } from '@/lib/status'

defineProps<{ priority: OrderPriority }>()
</script>

<template>
  <span
    v-if="priority !== OrderPriority.Normal"
    class="inline-flex items-center gap-1 text-xs font-medium"
    :class="PRIORITY_STYLES[priority]"
    :title="`Приоритет: ${PRIORITY_LABELS[priority]}`"
  >
    <svg class="size-3.5" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        v-if="priority === OrderPriority.High"
        d="M12 4v10m0 6h.01"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
      />
      <path v-else d="M6 12h12" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
    </svg>
    {{ PRIORITY_LABELS[priority] }}
  </span>
</template>
