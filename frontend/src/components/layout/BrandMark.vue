<script setup lang="ts">
import LogoMark from './LogoMark.vue'

withDefaults(defineProps<{ inverted?: boolean; compact?: boolean; size?: number }>(), {
  inverted: false,
  compact: false,
  size: 32,
})
</script>

<template>
  <span class="inline-flex items-center gap-2.5">
    <LogoMark :size="size" :inverted="inverted" />
    <span
      v-if="!compact"
      class="font-display text-lg font-extrabold tracking-tight"
      :class="inverted ? 'text-white' : 'text-ink'"
    >
      Repair<span :class="inverted ? 'text-brand-200' : 'text-accent-ink'">Flow</span>
    </span>
  </span>
</template>
