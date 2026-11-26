<script setup lang="ts">
import { CircleAlert, CircleCheck, Info, X } from '@lucide/vue'
import { t } from '@/i18n'
import { useToastStore } from '@/stores/toasts'

const toasts = useToastStore()

const KINDS = {
  success: { icon: CircleCheck, color: 'text-success-ink' },
  error: { icon: CircleAlert, color: 'text-danger-ink' },
  info: { icon: Info, color: 'text-accent-ink' },
} as const
</script>

<template>
  <!--
    На телефоне уведомления падают сверху, как системные баннеры: низ экрана занят меню
    и кнопками действий. На широком экране — в правом нижнем углу.
  -->
  <div
    class="pointer-events-none fixed inset-x-0 top-0 z-[60] flex flex-col items-center gap-2 px-3 pt-[max(0.75rem,env(safe-area-inset-top))] sm:top-auto sm:bottom-0 sm:items-end sm:p-6"
    role="status"
    aria-live="polite"
  >
    <TransitionGroup
      enter-active-class="transition duration-200 ease-[var(--ease-app)]"
      enter-from-class="-translate-y-3 opacity-0 sm:translate-y-3"
      leave-active-class="transition duration-150"
      leave-to-class="opacity-0 scale-95"
    >
      <div
        v-for="toast in toasts.items"
        :key="toast.id"
        class="pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-2xl border border-line bg-surface px-4 py-3 text-sm text-ink shadow-overlay"
      >
        <component
          :is="KINDS[toast.kind].icon"
          class="mt-px size-[18px] shrink-0"
          :class="KINDS[toast.kind].color"
          aria-hidden="true"
        />
        <span class="flex-1 leading-snug">{{ toast.text }}</span>
        <button
          type="button"
          class="-m-1 rounded-md p-1 text-ink-3 hover:text-ink"
          :aria-label="t('common.close')"
          @click="toasts.remove(toast.id)"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
