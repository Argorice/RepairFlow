<script setup lang="ts">
withDefaults(defineProps<{ lines?: number; class?: string }>(), { lines: 3 })
</script>

<template>
  <div class="space-y-3" :class="$props.class" aria-hidden="true">
    <div
      v-for="line in lines"
      :key="line"
      class="h-4 animate-pulse rounded bg-slate-200"
      :style="{ width: `${100 - (line % 3) * 12}%` }"
    />
  </div>
</template>
