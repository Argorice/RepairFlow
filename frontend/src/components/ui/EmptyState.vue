<script setup lang="ts">
import { Inbox } from '@lucide/vue'
import type { Component } from 'vue'

withDefaults(
  defineProps<{
    title: string
    description?: string
    icon?: Component
    compact?: boolean
  }>(),
  { icon: () => Inbox, compact: false },
)
</script>

<template>
  <div
    class="flex flex-col items-center justify-center gap-3 px-6 text-center"
    :class="compact ? 'py-8' : 'py-14'"
  >
    <div class="flex size-12 items-center justify-center rounded-2xl bg-surface-2">
      <component :is="icon" class="size-6 text-ink-3" aria-hidden="true" />
    </div>

    <div>
      <p class="font-display text-base font-semibold text-ink">{{ title }}</p>
      <p v-if="description" class="mt-1 max-w-sm text-sm text-ink-2">{{ description }}</p>
    </div>

    <slot />
  </div>
</template>
