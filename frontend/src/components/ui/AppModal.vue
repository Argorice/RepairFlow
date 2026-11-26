<script setup lang="ts">
import { X } from '@lucide/vue'
import { nextTick, onBeforeUnmount, onMounted, ref, useId } from 'vue'
import { t } from '@/i18n'

/**
 * Одно окно — два поведения. На телефоне это шторка снизу: до неё дотягивается большой палец,
 * а смахивание вниз закрывает её, как в нативных приложениях. На широком экране — обычный
 * диалог по центру.
 */
defineProps<{ title: string; description?: string }>()

const emit = defineEmits<{ close: [] }>()

const titleId = useId()
const panel = ref<HTMLElement | null>(null)

let restoreFocusTo: HTMLElement | null = null

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    emit('close')
  }
}

/* Смахивание вниз. Тянем за шапку шторки: прокрутке содержимого жест не мешает. */
const dragOffset = ref(0)
let dragStartY: number | null = null

const DISMISS_DISTANCE = 96

function onPointerDown(event: PointerEvent): void {
  if (event.pointerType === 'mouse') {
    return
  }

  dragStartY = event.clientY
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
}

function onPointerMove(event: PointerEvent): void {
  if (dragStartY === null) {
    return
  }

  dragOffset.value = Math.max(0, event.clientY - dragStartY)
}

function onPointerUp(): void {
  if (dragStartY === null) {
    return
  }

  const shouldClose = dragOffset.value > DISMISS_DISTANCE
  dragStartY = null
  dragOffset.value = 0

  if (shouldClose) {
    emit('close')
  }
}

onMounted(async () => {
  restoreFocusTo = document.activeElement as HTMLElement | null
  document.addEventListener('keydown', onKeydown)
  // Пока открыта шторка, страница под ней не прокручивается.
  document.body.style.overflow = 'hidden'

  await nextTick()

  // Мышь и клавиатура — сразу в первое поле. На телефоне так не делаем: фокус в поле
  // выдвинул бы клавиатуру поверх только что открытой шторки.
  const finePointer = window.matchMedia('(pointer: fine)').matches
  const firstField = finePointer ? panel.value?.querySelector<HTMLElement>('input, textarea, select') : null
  ;(firstField ?? panel.value)?.focus({ preventScroll: true })
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
  restoreFocusTo?.focus?.({ preventScroll: true })
})
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-6">
      <div class="absolute inset-0 animate-fade-in bg-[var(--rf-backdrop)]" aria-hidden="true" @click="emit('close')" />

      <div
        ref="panel"
        class="relative flex max-h-[92dvh] w-full animate-sheet-up flex-col rounded-t-3xl bg-surface shadow-overlay outline-none sm:max-w-lg sm:animate-pop-in sm:rounded-2xl"
        :style="dragOffset ? { transform: `translateY(${dragOffset}px)`, transition: 'none' } : undefined"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        tabindex="-1"
      >
        <header
          class="shrink-0 touch-none px-5 pt-3 sm:px-6 sm:pt-6"
          @pointerdown="onPointerDown"
          @pointermove="onPointerMove"
          @pointerup="onPointerUp"
          @pointercancel="onPointerUp"
        >
          <!-- Ручка шторки: подсказка, что её можно смахнуть -->
          <div class="mx-auto mb-3 h-1.5 w-10 rounded-full bg-line-strong sm:hidden" aria-hidden="true" />

          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2 :id="titleId" class="font-display text-lg font-semibold">{{ title }}</h2>
              <p v-if="description" class="mt-1 text-sm text-ink-2">{{ description }}</p>
            </div>

            <button
              type="button"
              class="pressable -mt-1 -mr-2 hidden rounded-lg p-2 text-ink-3 hover:bg-surface-2 hover:text-ink sm:block"
              :aria-label="t('common.close')"
              @click="emit('close')"
            >
              <X class="size-5" aria-hidden="true" />
            </button>
          </div>
        </header>

        <div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 py-4 sm:px-6">
          <slot />
        </div>

        <footer
          v-if="$slots.actions"
          class="flex shrink-0 flex-col-reverse gap-2 border-t border-line px-5 pt-4 pb-[max(1rem,env(safe-area-inset-bottom))] sm:flex-row sm:justify-end sm:gap-3 sm:border-0 sm:px-6 sm:pt-2 sm:pb-6 [&>*]:w-full sm:[&>*]:w-auto"
        >
          <slot name="actions" />
        </footer>
      </div>
    </div>
  </Teleport>
</template>
