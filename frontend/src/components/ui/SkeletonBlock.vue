<script setup lang="ts">
/**
 * Заглушка на время загрузки. Повторяет очертания будущего содержимого — строки или карточки,
 * — чтобы при появлении данных ничего не прыгало.
 */
withDefaults(defineProps<{ lines?: number; variant?: 'lines' | 'cards' | 'rows' }>(), {
  lines: 3,
  variant: 'lines',
})
</script>

<template>
  <div aria-hidden="true">
    <div v-if="variant === 'cards'" class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <div v-for="line in lines" :key="line" class="card space-y-3 p-5">
        <div class="h-3 w-1/2 animate-pulse rounded-full bg-surface-3" />
        <div class="h-7 w-2/3 animate-pulse rounded-lg bg-surface-3" />
      </div>
    </div>

    <div v-else-if="variant === 'rows'" class="divide-y divide-line">
      <div v-for="line in lines" :key="line" class="flex items-center gap-4 px-4 py-4">
        <div class="size-10 shrink-0 animate-pulse rounded-xl bg-surface-3" />
        <div class="flex-1 space-y-2">
          <div class="h-3.5 w-1/3 animate-pulse rounded-full bg-surface-3" />
          <div class="h-3 w-1/2 animate-pulse rounded-full bg-surface-2" />
        </div>
        <div class="h-6 w-20 animate-pulse rounded-full bg-surface-2" />
      </div>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="line in lines"
        :key="line"
        class="h-4 animate-pulse rounded-full bg-surface-3"
        :style="{ width: `${100 - (line % 3) * 14}%` }"
      />
    </div>
  </div>
</template>
