<script setup lang="ts">
import { computed } from 'vue'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md'

const props = withDefaults(
  defineProps<{
    variant?: Variant
    size?: Size
    loading?: boolean
    disabled?: boolean
    block?: boolean
    type?: 'button' | 'submit'
  }>(),
  {
    variant: 'primary',
    size: 'md',
    loading: false,
    disabled: false,
    block: false,
    type: 'button',
  },
)

const VARIANTS: Record<Variant, string> = {
  primary: 'bg-brand-600 text-white hover:bg-brand-700 disabled:bg-brand-600/50',
  secondary: 'bg-white text-slate-700 ring-1 ring-slate-200 hover:bg-slate-50',
  ghost: 'text-slate-600 hover:bg-slate-100',
  danger: 'bg-rose-600 text-white hover:bg-rose-700',
}

const SIZES: Record<Size, string> = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-10 px-4 text-sm',
}

const classes = computed(() => [
  'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors',
  'disabled:cursor-not-allowed disabled:opacity-60',
  VARIANTS[props.variant],
  SIZES[props.size],
  props.block ? 'w-full' : '',
])
</script>

<template>
  <button :type="type" :class="classes" :disabled="disabled || loading">
    <svg
      v-if="loading"
      class="size-4 animate-spin"
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 0 1 8-8v4a4 4 0 0 0-4 4H4z" />
    </svg>
    <slot />
  </button>
</template>
