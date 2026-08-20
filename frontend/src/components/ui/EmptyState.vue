<script setup lang="ts">
defineProps<{
  title: string
  description?: string
}>()
</script>

<template>
  <div class="flex flex-col items-center justify-center gap-3 px-6 py-16 text-center">
    <div class="flex size-12 items-center justify-center rounded-full bg-slate-100">
      <svg class="size-6 text-slate-400" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M4 7a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7Z"
          stroke="currentColor"
          stroke-width="1.5"
        />
      </svg>
    </div>

    <div>
      <p class="font-display text-base font-semibold text-slate-900">{{ title }}</p>
      <p v-if="description" class="mt-1 max-w-sm text-sm text-slate-500">{{ description }}</p>
    </div>

    <slot />
  </div>
</template>
