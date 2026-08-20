<script setup lang="ts">
import { computed, ref } from 'vue'
import type { DailyCountDto } from '@/api/types'
import { formatDay } from '@/lib/format'

const props = defineProps<{ daily: DailyCountDto[] }>()

/**
 * Две серии — значит идентичность, значит категориальная пара.
 * Индиго и изумруд проверены валидатором палитры: ΔE 31 для дейтеранопии, 37 для обычного зрения.
 * У изумруда контраст к светлой поверхности ниже 3:1, поэтому идентичность продублирована
 * легендой, подписями на концах линий и таблицей — цветом она нигде не держится в одиночку.
 */
const SERIES = [
  { key: 'created' as const, label: 'Создано', color: '#4F46E5' },
  { key: 'completed' as const, label: 'Выдано', color: '#10B981' },
]

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

/** Округляем верх шкалы до «круглого» числа — подписи оси должны читаться без усилий. */
const niceMax = computed(() => {
  const raw = maxValue.value
  const step = raw <= 4 ? 1 : raw <= 10 ? 2 : raw <= 30 ? 5 : 10
  return Math.ceil(raw / step) * step
})

const ticks = computed(() => {
  const count = 4
  return Array.from({ length: count + 1 }, (_, index) => Math.round((niceMax.value / count) * index))
})

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

  const items = SERIES.map((series) => ({
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

const totals = computed(() => ({
  created: points.value.reduce((sum, day) => sum + day.created, 0),
  completed: points.value.reduce((sum, day) => sum + day.completed, 0),
}))
</script>

<template>
  <section class="card p-5">
    <header class="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div>
        <h2 class="font-display text-base font-semibold">Заявки по дням</h2>
        <p class="mt-0.5 text-sm text-slate-500">
          Создано {{ totals.created }} · выдано {{ totals.completed }} за период
        </p>
      </div>

      <div class="flex items-center gap-4">
        <!-- Легенда обязательна при двух и более сериях: цвет не должен быть единственным ключом -->
        <ul class="flex items-center gap-4">
          <li v-for="series in SERIES" :key="series.key" class="flex items-center gap-1.5 text-xs text-slate-600">
            <span class="h-0.5 w-4 rounded-full" :style="{ backgroundColor: series.color }" />
            {{ series.label }}
          </li>
        </ul>

        <button
          class="text-xs text-slate-500 underline-offset-2 hover:text-slate-900 hover:underline"
          @click="showTable = !showTable"
        >
          {{ showTable ? 'График' : 'Таблицей' }}
        </button>
      </div>
    </header>

    <!-- На узком экране график не сжимается до нечитаемых подписей, а прокручивается вбок. -->
    <div v-if="!showTable" class="relative overflow-x-auto">
      <svg
        :viewBox="`0 0 ${WIDTH} ${HEIGHT}`"
        class="w-full min-w-[600px]"
        role="img"
        aria-label="График заявок по дням"
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
            stroke="#e2e8f0"
            stroke-width="1"
          />
          <text
            v-for="tick in ticks"
            :key="`tick-${tick}`"
            :x="PADDING.left - 8"
            :y="y(tick) + 4"
            text-anchor="end"
            class="fill-slate-400 text-[11px]"
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
          class="fill-slate-400 text-[11px]"
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
          stroke="#94a3b8"
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
            stroke="#ffffff"
            stroke-width="2"
          />
          <line
            v-if="Math.abs(item.labelY - item.dotY) > 1"
            :x1="x(points.length - 1) + 5"
            :y1="item.dotY"
            :x2="x(points.length - 1) + 10"
            :y2="item.labelY"
            stroke="#cbd5e1"
            stroke-width="1"
          />
          <text
            :x="x(points.length - 1) + 12"
            :y="item.labelY + 4"
            class="fill-slate-500 text-[11px]"
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
          @mouseenter="activeIndex = index"
          @mouseleave="activeIndex = null"
        />
      </svg>

      <!-- Подсказка дополняет значения, а не заменяет их: те же числа есть в таблице -->
      <div
        v-if="active"
        class="pointer-events-none absolute top-2 rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs shadow-[var(--shadow-overlay)]"
        :style="{ left: `calc(${(x(activeIndex ?? 0) / WIDTH) * 100}% + 8px)` }"
      >
        <p class="font-medium text-slate-900">{{ formatDay(active.date) }}</p>
        <p v-for="series in SERIES" :key="series.key" class="mt-1 flex items-center gap-1.5 text-slate-600">
          <span class="size-1.5 rounded-full" :style="{ backgroundColor: series.color }" />
          {{ series.label }}: <span class="tabular">{{ active[series.key] }}</span>
        </p>
      </div>
    </div>

    <!-- Табличный двойник графика -->
    <div v-else class="max-h-72 overflow-auto rounded-lg border border-slate-200">
      <table class="w-full text-left text-sm">
        <thead class="sticky top-0 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th class="px-3 py-2 font-medium">Дата</th>
            <th class="px-3 py-2 text-right font-medium">Создано</th>
            <th class="px-3 py-2 text-right font-medium">Выдано</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          <tr v-for="day in points" :key="day.date">
            <td class="px-3 py-1.5 text-slate-600">{{ formatDay(day.date) }}</td>
            <td class="px-3 py-1.5 text-right tabular">{{ day.created }}</td>
            <td class="px-3 py-1.5 text-right tabular">{{ day.completed }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
