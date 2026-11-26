<script setup lang="ts">
import { Check } from '@lucide/vue'
import { computed } from 'vue'
import type { OrderStatus, OrderStatusHistoryDto } from '@/api/types'
import { t } from '@/i18n'
import { formatDay, formatDateTime } from '@/lib/format'
import { MAIN_PATH, TERMINAL_STATUSES, statusLabel, statusStyle } from '@/lib/status'

const props = defineProps<{
  status: OrderStatus
  history: OrderStatusHistoryDto[]
}>()

type StepState = 'done' | 'current' | 'future'

interface Step {
  status: OrderStatus
  at: string | null
  author: string | null
  state: StepState
}

/**
 * Та самая деталь, которая запоминается: путь заявки нарисован шкалой, а не выпадающим списком.
 * Пройденные шаги залиты цветом своего статуса, текущий пульсирует, будущие — контурные.
 * Отмена или отказ встают последним шагом там, где путь оборвался.
 */
const steps = computed<Step[]>(() => {
  const reached = new Map<OrderStatus, OrderStatusHistoryDto>()

  for (const entry of props.history) {
    if (!reached.has(entry.toStatus)) {
      reached.set(entry.toStatus, entry)
    }
  }

  const isTerminal = TERMINAL_STATUSES.includes(props.status)

  const reachedIndexes = MAIN_PATH.map((status, index) => (reached.has(status) ? index : -1))
  const currentIndex = isTerminal ? Math.max(0, ...reachedIndexes) : MAIN_PATH.indexOf(props.status)

  const path = isTerminal ? MAIN_PATH.slice(0, currentIndex + 1) : MAIN_PATH

  const result: Step[] = path.map((status, index) => {
    const entry = reached.get(status) ?? null

    const state: StepState = isTerminal
      ? 'done'
      : index < currentIndex
        ? 'done'
        : index === currentIndex
          ? 'current'
          : 'future'

    return {
      status,
      at: entry?.changedAt ?? null,
      author: entry?.changedBy.fullName ?? null,
      state,
    }
  })

  if (isTerminal) {
    const entry = reached.get(props.status) ?? null

    result.push({
      status: props.status,
      at: entry?.changedAt ?? null,
      author: entry?.changedBy.fullName ?? null,
      state: 'current',
    })
  }

  return result
})

const currentIndex = computed(() => steps.value.findIndex((step) => step.state === 'current'))
const current = computed(() => steps.value[currentIndex.value] ?? null)
const next = computed(() => steps.value[currentIndex.value + 1] ?? null)
</script>

<template>
  <!-- Телефон: компактная полоса прогресса и текущий шаг словами -->
  <div class="sm:hidden">
    <div class="flex items-baseline justify-between gap-3 text-sm">
      <p class="font-medium text-ink">
        {{ t('timeline.step', { current: currentIndex + 1, total: steps.length }) }}
        <span v-if="current" class="text-ink-2">· {{ statusLabel(current.status) }}</span>
      </p>
      <p v-if="current?.at" class="shrink-0 text-xs text-ink-3 tabular">{{ formatDay(current.at) }}</p>
    </div>

    <ol class="mt-3 flex gap-1" :aria-label="t('timeline.label')">
      <li
        v-for="step in steps"
        :key="step.status"
        class="h-1.5 flex-1 rounded-full"
        :class="step.state === 'future' ? 'bg-surface-3' : statusStyle(step.status).solid"
      >
        <span class="sr-only">
          {{ statusLabel(step.status) }} — {{ t(`timeline.states.${step.state}`) }}
        </span>
      </li>
    </ol>

    <p v-if="next" class="mt-2 text-xs text-ink-3">
      {{ t('timeline.next', { status: statusLabel(next.status) }) }}
    </p>
  </div>

  <!-- Широкий экран: полная шкала с датами и авторами переходов -->
  <ol class="hidden sm:flex" :aria-label="t('timeline.label')">
    <li v-for="(step, index) in steps" :key="step.status" class="relative flex min-w-0 flex-1 flex-col gap-2.5">
      <span
        v-if="index > 0"
        aria-hidden="true"
        class="absolute top-[11px] right-[calc(100%-12px)] h-0.5 w-full"
        :class="step.state === 'future' ? 'bg-surface-3' : statusStyle(step.status).solid"
      />

      <span class="relative z-10 flex size-6 items-center justify-center">
        <span
          v-if="step.state === 'current'"
          class="absolute size-6 animate-ping rounded-full opacity-25"
          :class="statusStyle(step.status).solid"
        />
        <span
          class="flex items-center justify-center rounded-full"
          :class="
            step.state === 'future'
              ? 'size-3 bg-surface ring-2 ring-line-strong'
              : step.state === 'current'
                ? ['size-4 ring-4 ring-surface', statusStyle(step.status).solid]
                : ['size-5 text-white', statusStyle(step.status).solid]
          "
        >
          <Check v-if="step.state === 'done'" class="size-3" stroke-width="3" aria-hidden="true" />
        </span>
      </span>

      <div class="min-w-0 pr-3">
        <p
          class="line-clamp-2 text-sm leading-snug font-medium"
          :class="step.state === 'future' ? 'text-ink-3' : 'text-ink'"
          :title="statusLabel(step.status)"
        >
          {{ statusLabel(step.status) }}
        </p>
        <p v-if="step.at" class="truncate text-xs text-ink-3 tabular" :title="formatDateTime(step.at)">
          {{ formatDay(step.at) }}
        </p>
        <p v-if="step.author" class="truncate text-xs text-ink-3">{{ step.author }}</p>
      </div>
    </li>
  </ol>
</template>
