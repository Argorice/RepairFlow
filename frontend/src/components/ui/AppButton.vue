<script setup lang="ts">
import { LoaderCircle } from '@lucide/vue'
import { computed, type Component } from 'vue'

type Variant = 'primary' | 'secondary' | 'soft' | 'ghost' | 'danger'
type Size = 'sm' | 'md' | 'lg'

const props = withDefaults(
  defineProps<{
    variant?: Variant
    size?: Size
    loading?: boolean
    disabled?: boolean
    block?: boolean
    /** Иконка перед текстом. Для кнопки-иконки без текста — вместе с `square` и `label`. */
    icon?: Component
    square?: boolean
    /** Доступное имя для кнопки без текста: экранный диктор прочитает его вместо иконки. */
    label?: string
    type?: 'button' | 'submit'
  }>(),
  {
    variant: 'primary',
    size: 'md',
    loading: false,
    disabled: false,
    block: false,
    square: false,
    type: 'button',
  },
)

const VARIANTS: Record<Variant, string> = {
  primary: 'bg-accent text-on-accent hover:bg-accent-hover',
  secondary: 'bg-surface text-ink ring-1 ring-inset ring-line hover:bg-surface-2 hover:ring-line-strong',
  soft: 'bg-accent-soft text-accent-ink hover:bg-accent-soft/70',
  ghost: 'text-ink-2 hover:bg-surface-2 hover:text-ink',
  danger: 'bg-danger-soft text-danger-ink ring-1 ring-inset ring-danger-line hover:bg-danger-soft/70',
}

/** На телефоне кнопки выше: палец промахивается чаще курсора, 44px — минимум по гайдлайнам. */
const SIZES: Record<Size, { base: string; square: string; icon: string }> = {
  sm: { base: 'h-9 px-3 text-sm gap-1.5', square: 'size-9', icon: 'size-4' },
  md: { base: 'h-11 px-4 text-sm gap-2 sm:h-10', square: 'size-11 sm:size-10', icon: 'size-[18px]' },
  lg: { base: 'h-12 px-5 text-base gap-2', square: 'size-12', icon: 'size-5' },
}

const classes = computed(() => [
  'pressable inline-flex shrink-0 select-none items-center justify-center rounded-xl font-medium transition-colors',
  'disabled:pointer-events-none disabled:opacity-50',
  VARIANTS[props.variant],
  props.square ? SIZES[props.size].square : SIZES[props.size].base,
  props.block ? 'w-full' : '',
])
</script>

<template>
  <button
    :type="type"
    :class="classes"
    :disabled="disabled || loading"
    :aria-label="label"
    :aria-busy="loading || undefined"
  >
    <LoaderCircle v-if="loading" :class="SIZES[size].icon" class="animate-spin" aria-hidden="true" />
    <component :is="icon" v-else-if="icon" :class="SIZES[size].icon" aria-hidden="true" />
    <slot />
  </button>
</template>
