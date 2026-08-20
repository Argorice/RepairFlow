<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue'

defineProps<{ title: string; description?: string }>()

const emit = defineEmits<{ close: [] }>()

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    emit('close')
  }
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => document.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div class="fixed inset-0 z-40 flex items-end justify-center p-4 sm:items-center">
    <div class="absolute inset-0 bg-slate-900/40" @click="emit('close')" />

    <div
      class="relative w-full max-w-lg rounded-xl bg-white p-6 shadow-[var(--shadow-overlay)]"
      role="dialog"
      aria-modal="true"
    >
      <h2 class="font-display text-lg font-semibold">{{ title }}</h2>
      <p v-if="description" class="mt-1 text-sm text-slate-500">{{ description }}</p>

      <div class="mt-4">
        <slot />
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <slot name="actions" />
      </div>
    </div>
  </div>
</template>
