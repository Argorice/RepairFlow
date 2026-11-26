<script setup lang="ts">
import type { Component } from 'vue'

withDefaults(
  defineProps<{
    label: string
    value: string
    hint?: string
    icon?: Component
    /** Мягкая подсветка для метрики, требующей внимания. Не для «просто красиво». */
    tone?: 'neutral' | 'warning'
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <div class="card p-4 sm:p-5" :class="tone === 'warning' ? 'bg-warn-soft ring-1 ring-warn-line' : ''">
    <div class="flex items-center gap-2">
      <span
        v-if="icon"
        class="flex size-7 items-center justify-center rounded-lg"
        :class="tone === 'warning' ? 'bg-surface text-warn-ink' : 'bg-accent-soft text-accent-ink'"
        aria-hidden="true"
      >
        <component :is="icon" class="size-4" />
      </span>
      <p class="truncate text-sm text-ink-2">{{ label }}</p>
    </div>

    <!--
      Значение плитки — обычный гротеск, а не заголовочный шрифт: крупная цифра
      в декоративном начертании читается как украшение. И пропорциональные цифры,
      без tabular-nums: на этом кегле равная ширина знаков делает число рыхлым.
    -->
    <p class="mt-3 truncate text-2xl font-semibold text-ink sm:text-3xl">{{ value }}</p>

    <p v-if="hint" class="mt-1 truncate text-xs" :class="tone === 'warning' ? 'text-warn-ink' : 'text-ink-3'">
      {{ hint }}
    </p>
  </div>
</template>
