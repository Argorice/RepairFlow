<script setup lang="ts">
/** Чип быстрого фильтра. Нажатый чип — тот же фильтр, что и в форме, просто ближе к пальцу. */
defineProps<{ active: boolean; count?: number }>()
</script>

<template>
  <button
    type="button"
    :aria-pressed="active"
    class="pressable inline-flex h-9 shrink-0 items-center gap-1.5 rounded-full px-3.5 text-sm font-medium whitespace-nowrap ring-1 transition-colors ring-inset"
    :class="
      active
        ? 'bg-accent text-on-accent ring-accent'
        : 'bg-surface text-ink-2 ring-line hover:text-ink hover:ring-line-strong'
    "
  >
    <slot />
    <span
      v-if="count !== undefined"
      class="tabular text-xs"
      :class="active ? 'text-on-accent/80' : 'text-ink-3'"
    >
      {{ count }}
    </span>
  </button>
</template>
