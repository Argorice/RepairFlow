<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DailyCountDto } from '@/api/types'
import { t } from '@/i18n'
import { formatDay } from '@/lib/format'

const props = defineProps<{ daily: DailyCountDto[] }>()

/**
 * Две серии — значит идентичность, значит категориальная пара.
 * Индиго и изумруд проверены валидатором палитры: ΔE 31 для дейтеранопии, 37 для обычного зрения.
 * У изумруда контраст к светлой поверхности ниже 3:1, поэтому идентичность продублирована
 * легендой, подписями на концах линий и таблицей — цветом она нигде не держится в одиночку.
 */
const SERIES = computed(() => [
  { key: 'created' as const, label: t('dashboard.seriesCreated'), color: 'var(--rf-chart-1)' },
  { key: 'completed' as const, label: t('dashboard.seriesCompleted'), color: 'var(--rf-chart-2)' },
])

const WIDTH = 720
const HEIGHT = 240
const PADDING = { top: 16, right: 84, bottom: 28, left: 36 }

const showTable = ref(false)
const activeIndex = ref<number | null>(null)

const points = computed(() => props.daily)

const maxValue = computed(() => {
  const values = points.value.flatMap((day) => [day.created, day.completed])
  return Math.max(1, ...values)
})

/**
 * Шаг шкалы — «круглое» число, а верх шкалы кратен шагу: подписи оси читаются без усилий
 * и всегда целые (полторы заявки не бывает).
 */
const tickStep = computed(() => {
  const raw = maxValue.value
  return raw <= 4 ? 1 : raw <= 10 ? 2 : raw <= 30 ? 5 : 10
})

const niceMax = computed(() => Math.ceil(maxValue.value / tickStep.value) * tickStep.value)

const ticks = computed(() =>
  Array.from({ length: niceMax.value / tickStep.value + 1 }, (_, index) => index * tickStep.value),
)

function x(index: number): number {
  const span = Math.max(points.value.length - 1, 1)
  return PADDING.left + (index / span) * (WIDTH - PADDING.left - PADDING.right)
}

function y(value: number): number {
  const usable = HEIGHT - PADDING.top - PADDING.bottom
  return PADDING.top + usable - (value / niceMax.value) * usable
}

function line(key: 'created' | 'completed'): string {
  return points.value.map((day, index) => `${x(index)},${y(day[key])}`).join(' ')
}

const xLabels = computed(() => {
  const total = points.value.length
  const every = Math.max(1, Math.ceil(total / 6))

  return points.value
    .map((day, index) => ({ index, date: day.date }))
    .filter((item) => item.index % every === 0 || item.index === total - 1)
})

/**
 * Подписи на концах линий. Если серии сходятся, подписи налезают друг на друга —
 * тогда разводим их и дотягиваем тонкой выноской до своей точки, чтобы связь не потерялась.
 */
const MIN_LABEL_GAP = 14

const endLabels = computed(() => {
  const last = points.value[points.value.length - 1]
  if (!last) {
    return []
  }

  const items = SERIES.value.map((series) => ({
    key: series.key,
    label: series.label,
    color: series.color,
    dotY: y(last[series.key]),
    labelY: y(last[series.key]),
  })).sort((first, second) => first.dotY - second.dotY)

  const gap = (items[1]?.labelY ?? 0) - (items[0]?.labelY ?? 0)

  if (items.length === 2 && gap < MIN_LABEL_GAP) {
    const shift = (MIN_LABEL_GAP - gap) / 2
    items[0]!.labelY -= shift
    items[1]!.labelY += shift
  }

  return items
})

const active = computed(() => (activeIndex.value === null ? null : points.value[activeIndex.value] ?? null))

/** Подсказка не должна уезжать за правый край: у последних точек она открывается влево. */
const tooltipPosition = computed(() => {
  const ratio = x(activeIndex.value ?? 0) / WIDTH
  return ratio > 0.6
    ? { right: `calc(${(1 - ratio) * 100}% + 8px)` }
    : { left: `calc(${ratio * 100}% + 8px)` }
})

const totals = computed(() => ({
  created: points.value.reduce((sum, day) => sum + day.created, 0),
  completed: points.value.reduce((sum, day) => sum + day.completed, 0),
}))
</script>

<template>
  <section class="card p-4 sm:p-5">
    <header class="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div>
        <h2 class="section-title">{{ t('dashboard.chartTitle') }}</h2>
        <p class="mt-0.5 text-sm text-ink-2">
          {{ t('dashboard.chartTotals', { created: totals.created, completed: totals.completed }) }}
        </p>
      </div>

      <div class="flex items-center gap-4">
        <!-- Легенда обязательна при двух и более сериях: цвет не должен быть единственным ключом -->
        <ul class="flex items-center gap-4">
          <li v-for="series in SERIES" :key="series.key" class="flex items-center gap-1.5 text-xs text-ink-2">
            <span class="h-0.5 w-4 rounded-full" :style="{ backgroundColor: series.color }" />
            {{ series.label }}
          </li>
        </ul>

        <button
          type="button"
          class="rounded-md text-xs text-ink-2 underline-offset-2 hover:text-ink hover:underline"
          @click="showTable = !showTable"
        >
          {{ showTable ? t('dashboard.asChart') : t('dashboard.asTable') }}
        </button>
      </div>
    </header>

    <!-- На узком экране график не сжимается до нечитаемых подписей, а прокручивается вбок. -->
    <div v-if="!showTable" class="relative overflow-x-auto">
      <svg
        :viewBox="`0 0 ${WIDTH} ${HEIGHT}`"
        class="w-full min-w-[600px]"
        role="img"
        :aria-label="t('dashboard.chartTitle')"
      >
        <!-- Сетка: сплошные волосяные линии на один шаг от фона -->
        <g>
          <line
            v-for="tick in ticks"
            :key="`grid-${tick}`"
            :x1="PADDING.left"
            :x2="WIDTH - PADDING.right"
            :y1="y(tick)"
            :y2="y(tick)"
            stroke="var(--rf-chart-grid)"
            stroke-width="1"
          />
          <text
            v-for="tick in ticks"
            :key="`tick-${tick}`"
            :x="PADDING.left - 8"
            :y="y(tick) + 4"
            text-anchor="end"
            class="fill-ink-3 text-[11px]"
            style="font-variant-numeric: tabular-nums"
          >
            {{ tick }}
          </text>
        </g>

        <!-- Подписи дат -->
        <text
          v-for="label in xLabels"
          :key="`x-${label.index}`"
          :x="x(label.index)"
          :y="HEIGHT - 8"
          text-anchor="middle"
          class="fill-ink-3 text-[11px]"
        >
          {{ formatDay(label.date) }}
        </text>

        <!-- Вертикаль под курсором -->
        <line
          v-if="activeIndex !== null"
          :x1="x(activeIndex)"
          :x2="x(activeIndex)"
          :y1="PADDING.top"
          :y2="HEIGHT - PADDING.bottom"
          stroke="var(--rf-chart-cursor)"
          stroke-width="1"
        />

        <!-- Линии серий: 2px, скруглённые стыки -->
        <polyline
          v-for="series in SERIES"
          :key="series.key"
          :points="line(series.key)"
          fill="none"
          :stroke="series.color"
          stroke-width="2"
          stroke-linejoin="round"
          stroke-linecap="round"
        />

        <!-- Точка на конце каждой серии и подпись рядом: идентичность держится не только цветом -->
        <template v-for="item in endLabels" :key="`end-${item.key}`">
          <circle
            :cx="x(points.length - 1)"
            :cy="item.dotY"
            r="4"
            :fill="item.color"
            stroke="var(--rf-surface)"
            stroke-width="2"
          />
          <line
            v-if="Math.abs(item.labelY - item.dotY) > 1"
            :x1="x(points.length - 1) + 5"
            :y1="item.dotY"
            :x2="x(points.length - 1) + 10"
            :y2="item.labelY"
            stroke="var(--rf-line-strong)"
            stroke-width="1"
          />
          <text
            :x="x(points.length - 1) + 12"
            :y="item.labelY + 4"
            class="fill-ink-2 text-[11px]"
          >
            {{ item.label }}
          </text>
        </template>

        <!-- Зоны наведения шире самих точек: попасть должно быть легко -->
        <rect
          v-for="(day, index) in points"
          :key="`hit-${day.date}`"
          :x="x(index) - (WIDTH - PADDING.left - PADDING.right) / Math.max(points.length, 1) / 2"
          :y="PADDING.top"
          :width="(WIDTH - PADDING.left - PADDING.right) / Math.max(points.length, 1)"
          :height="HEIGHT - PADDING.top - PADDING.bottom"
          fill="transparent"
          @pointerenter="activeIndex = index"
          @pointerdown="activeIndex = index"
          @pointerleave="activeIndex = null"
        />
      </svg>

      <!-- Подсказка дополняет значения, а не заменяет их: те же числа есть в таблице -->
      <div
        v-if="active"
        class="pointer-events-none absolute top-2 rounded-xl border border-line bg-surface px-3 py-2 text-xs shadow-overlay"
        :style="tooltipPosition"
      >
        <p class="font-medium text-ink">{{ formatDay(active.date) }}</p>
        <p v-for="series in SERIES" :key="series.key" class="mt-1 flex items-center gap-1.5 whitespace-nowrap text-ink-2">
          <span class="size-1.5 rounded-full" :style="{ backgroundColor: series.color }" />
          {{ series.label }}: <span class="tabular">{{ active[series.key] }}</span>
        </p>
      </div>
    </div>

    <!-- Табличный двойник графика -->
    <div v-else class="max-h-72 overflow-auto rounded-xl border border-line">
      <table class="w-full text-left text-sm">
        <thead class="sticky top-0 bg-surface-2 text-xs tracking-wide text-ink-3 uppercase">
          <tr>
            <th class="px-3 py-2 font-medium">{{ t('dashboard.date') }}</th>
            <th class="px-3 py-2 text-right font-medium">{{ t('dashboard.seriesCreated') }}</th>
            <th class="px-3 py-2 text-right font-medium">{{ t('dashboard.seriesCompleted') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-line">
          <tr v-for="day in points" :key="day.date">
            <td class="px-3 py-1.5 text-ink-2">{{ formatDay(day.date) }}</td>
            <td class="px-3 py-1.5 text-right tabular">{{ day.created }}</td>
            <td class="px-3 py-1.5 text-right tabular">{{ day.completed }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
