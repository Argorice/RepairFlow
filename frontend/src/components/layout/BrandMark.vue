<script setup lang="ts">
import { Wrench } from '@lucide/vue'

withDefaults(defineProps<{ inverted?: boolean; compact?: boolean }>(), {
  inverted: false,
  compact: false,
})
</script>

<template>
  <span class="inline-flex items-center gap-2.5">
    <span
      class="flex size-8 items-center justify-center rounded-[10px]"
      :class="inverted ? 'bg-white text-brand-600' : 'bg-accent text-on-accent'"
      aria-hidden="true"
    >
      <Wrench class="size-[18px]" stroke-width="2.25" />
    </span>
    <span
      v-if="!compact"
      class="font-display text-lg font-extrabold tracking-tight"
      :class="inverted ? 'text-white' : 'text-ink'"
    >
      RepairFlow
    </span>
  </span>
</template>
