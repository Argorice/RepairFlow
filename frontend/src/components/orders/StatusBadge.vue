<script setup lang="ts">
import { computed } from 'vue'
import type { OrderStatus } from '@/api/types'
import { STATUS_LABELS, statusStyle } from '@/lib/status'

const props = withDefaults(defineProps<{ status: OrderStatus; label?: string }>(), {})

const style = computed(() => statusStyle(props.status))
const text = computed(() => props.label ?? STATUS_LABELS[props.status])
</script>

<template>
  <span
    class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset"
    :class="style.badge"
  >
    <span class="size-1.5 rounded-full" :class="style.solid" />
    {{ text }}
  </span>
</template>
