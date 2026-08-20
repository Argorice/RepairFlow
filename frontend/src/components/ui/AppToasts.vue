<script setup lang="ts">
import { useToastStore } from '@/stores/toasts'

const toasts = useToastStore()

const STYLES: Record<string, string> = {
  success: 'border-emerald-200 bg-emerald-50 text-emerald-800',
  error: 'border-rose-200 bg-rose-50 text-rose-800',
  info: 'border-slate-200 bg-white text-slate-700',
}
</script>

<template>
  <div
    class="pointer-events-none fixed inset-x-0 bottom-0 z-50 flex flex-col items-center gap-2 p-4 sm:items-end"
    role="status"
    aria-live="polite"
  >
    <TransitionGroup
      enter-active-class="transition duration-200"
      enter-from-class="translate-y-2 opacity-0"
      leave-active-class="transition duration-150"
      leave-to-class="opacity-0"
    >
      <div
        v-for="toast in toasts.items"
        :key="toast.id"
        class="pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border px-4 py-3 text-sm shadow-[var(--shadow-overlay)]"
        :class="STYLES[toast.kind]"
      >
        <span class="flex-1">{{ toast.text }}</span>
        <button
          class="text-current/60 hover:text-current"
          aria-label="Закрыть"
          @click="toasts.remove(toast.id)"
        >
          ×
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
