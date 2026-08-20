<script setup lang="ts">
import { computed } from 'vue'
import type { OrderStatus, OrderStatusHistoryDto } from '@/api/types'
import { formatDateTime } from '@/lib/format'
import { MAIN_PATH, STATUS_LABELS, TERMINAL_STATUSES, statusStyle } from '@/lib/status'

const props = defineProps<{
  status: OrderStatus
  history: OrderStatusHistoryDto[]
}>()

type StepState = 'done' | 'current' | 'future'

interface Step {
  status: OrderStatus
  label: string
  at: string | null
  author: string | null
  state: StepState
}

/**
 * Та самая деталь, которая запоминается: путь заявки нарисован шкалой, а не выпадающим списком.
 * Пройденные шаги залиты цветом своего статуса, текущий пульсирует, будущие — контурные.
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
  const currentIndex = isTerminal
    ? Math.max(0, ...reachedIndexes)
    : MAIN_PATH.indexOf(props.status)

  const result: Step[] = MAIN_PATH.map((status, index) => {
    const entry = reached.get(status) ?? null

    const state: StepState = isTerminal
      ? index <= currentIndex
        ? 'done'
        : 'future'
      : index < currentIndex
        ? 'done'
        : index === currentIndex
          ? 'current'
          : 'future'

    return {
      status,
      label: STATUS_LABELS[status],
      at: entry?.changedAt ?? null,
      author: entry?.changedBy.fullName ?? null,
      state,
    }
  })

  if (isTerminal) {
    const entry = reached.get(props.status) ?? null

    result.push({
      status: props.status,
      label: STATUS_LABELS[props.status],
      at: entry?.changedAt ?? null,
      author: entry?.changedBy.fullName ?? null,
      state: 'current',
    })
  }

  return result
})
</script>

<template>
  <ol class="flex flex-col gap-4 sm:flex-row sm:gap-0">
    <li
      v-for="(step, index) in steps"
      :key="step.status"
      class="relative flex gap-3 sm:flex-1 sm:flex-col sm:gap-2"
    >
      <!-- Соединительная линия: горизонтальная на широком экране, вертикальная на мобильном -->
      <span
        v-if="index > 0"
        aria-hidden="true"
        class="absolute left-[11px] top-[-16px] h-4 w-0.5 sm:left-auto sm:right-1/2 sm:top-[11px] sm:h-0.5 sm:w-full"
        :class="step.state === 'future' ? 'bg-slate-200' : statusStyle(step.status).solid"
      />

      <span class="relative z-10 flex size-6 shrink-0 items-center justify-center">
        <span
          v-if="step.state === 'current'"
          class="absolute size-6 animate-ping rounded-full opacity-30"
          :class="statusStyle(step.status).solid"
        />
        <span
          class="size-3 rounded-full ring-4 ring-white"
          :class="
            step.state === 'future'
              ? 'bg-white outline outline-2 outline-slate-200'
              : statusStyle(step.status).solid
          "
        />
      </span>

      <div class="min-w-0 sm:pr-4">
        <p
          class="truncate text-sm font-medium"
          :class="step.state === 'future' ? 'text-slate-400' : 'text-slate-900'"
        >
          {{ step.label }}
        </p>
        <p v-if="step.at" class="truncate text-xs text-slate-500 tabular">
          {{ formatDateTime(step.at) }}
        </p>
        <p v-if="step.author" class="truncate text-xs text-slate-400">{{ step.author }}</p>
      </div>
    </li>
  </ol>
</template>
