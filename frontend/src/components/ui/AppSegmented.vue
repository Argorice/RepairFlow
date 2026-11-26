<script setup lang="ts" generic="T extends string">
import type { Component } from 'vue'

/**
 * Сегментированный переключатель — выбор одного из нескольких равноправных вариантов:
 * тема, язык, период, вкладки карточки. Ведёт себя как радиогруппа: стрелки двигают выбор.
 */
withDefaults(
  defineProps<{
    options: { value: T; label: string; icon?: Component; badge?: number | string }[]
    label: string
    /** Растянуть на всю ширину, поделив поровну; 'mobile' — только на узком экране. */
    block?: boolean | 'mobile'
    size?: 'sm' | 'md'
  }>(),
  { block: false, size: 'md' },
)

const model = defineModel<T>({ required: true })

function onKeydown(event: KeyboardEvent, options: { value: T }[], index: number): void {
  const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0
  if (step === 0) {
    return
  }

  event.preventDefault()
  const next = options[(index + step + options.length) % options.length]
  if (next) {
    model.value = next.value
    const buttons = (event.currentTarget as HTMLElement).parentElement?.querySelectorAll('button')
    buttons?.[(index + step + options.length) % options.length]?.focus()
  }
}
</script>

<template>
  <div
    role="radiogroup"
    :aria-label="label"
    class="max-w-full gap-1 overflow-x-auto rounded-xl bg-surface-2 p-1 scrollbar-none"
    :class="block === 'mobile' ? 'flex w-full sm:inline-flex sm:w-auto' : block ? 'flex w-full' : 'inline-flex'"
  >
    <button
      v-for="(option, index) in options"
      :key="option.value"
      type="button"
      role="radio"
      :aria-checked="model === option.value"
      :tabindex="model === option.value ? 0 : -1"
      class="pressable inline-flex shrink-0 items-center justify-center gap-1.5 rounded-lg px-3 font-medium whitespace-nowrap transition-colors"
      :class="[
        block === 'mobile' ? 'flex-1 sm:flex-none' : block ? 'flex-1' : '',
        size === 'sm' ? 'h-8 text-xs' : 'h-9 text-sm',
        model === option.value
          ? 'bg-surface text-ink shadow-[0_1px_2px_rgb(15_23_42/0.08)] ring-1 ring-line dark:ring-line-strong'
          : 'text-ink-2 hover:text-ink',
      ]"
      @click="model = option.value"
      @keydown="onKeydown($event, options, index)"
    >
      <component :is="option.icon" v-if="option.icon" class="size-4" aria-hidden="true" />
      {{ option.label }}
      <span
        v-if="option.badge"
        class="min-w-5 rounded-full px-1.5 text-[11px] leading-5 tabular"
        :class="model === option.value ? 'bg-accent-soft text-accent-ink' : 'bg-surface-3 text-ink-2'"
      >
        {{ option.badge }}
      </span>
    </button>
  </div>
</template>
