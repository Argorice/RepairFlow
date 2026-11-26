<script setup lang="ts">
import { computed } from 'vue'
import { initials } from '@/lib/format'

const props = withDefaults(
  defineProps<{ name: string; size?: 'sm' | 'md' | 'lg'; tone?: 'accent' | 'neutral' | 'warn' }>(),
  { size: 'md', tone: 'accent' },
)

const SIZES = { sm: 'size-8 text-xs', md: 'size-10 text-sm', lg: 'size-14 text-lg' }
const TONES = {
  accent: 'bg-accent-soft text-accent-ink',
  neutral: 'bg-surface-3 text-ink-2',
  warn: 'bg-warn-soft text-warn-ink',
}

const letters = computed(() => initials(props.name) || '?')
</script>

<template>
  <span
    class="inline-flex shrink-0 items-center justify-center rounded-full font-semibold select-none"
    :class="[SIZES[size], TONES[tone]]"
    aria-hidden="true"
  >
    {{ letters }}
  </span>
</template>
