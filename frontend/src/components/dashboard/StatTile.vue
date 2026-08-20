<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    value: string
    hint?: string
    /** Мягкая подсветка для метрики, требующей внимания. Не для «просто красиво». */
    tone?: 'neutral' | 'warning'
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <div class="card p-5" :class="tone === 'warning' ? 'border-amber-200 bg-amber-50/60' : ''">
    <p class="text-sm text-slate-500">{{ label }}</p>

    <!--
      Значение плитки — обычный гротеск, а не заголовочный шрифт: крупная цифра
      в декоративном начертании читается как украшение. И пропорциональные цифры,
      без tabular-nums: на этом кегле равная ширина знаков делает число рыхлым.
    -->
    <p class="mt-2 text-3xl font-semibold text-slate-900">{{ value }}</p>

    <p v-if="hint" class="mt-1 text-xs" :class="tone === 'warning' ? 'text-amber-700' : 'text-slate-400'">
      {{ hint }}
    </p>
  </div>
</template>
