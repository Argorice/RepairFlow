<script setup lang="ts">
/**
 * Знак RepairFlow: гайка — ремонт, шеврон внутри — движение заявки вперёд по шагам.
 * Нарисован для маленьких размеров: на 16 px вкладки браузера читаются и гайка, и стрелка.
 *
 * От 40 px появляется фаска — тонкий шестиугольник внутри гайки. Без неё крупный знак
 * похож на кнопку «далее», с ней узнаётся гайка. На мелких размерах фаска сливается
 * в грязь, поэтому там знак остаётся плоским.
 *
 * `turning` — гайка проворачивается рывками, как под ключом с трещоткой. Это вся анимация
 * загрузочного экрана: шестиугольник симметричен на 60°, поэтому цикл бесшовный.
 */
import { computed } from 'vue'

const props = withDefaults(defineProps<{ size?: number; inverted?: boolean; turning?: boolean }>(), {
  size: 32,
  inverted: false,
  turning: false,
})

const chamfered = computed(() => props.size >= 40)
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 32 32"
    class="shrink-0"
    :class="inverted ? 'logo-inverted' : 'logo-default'"
    aria-hidden="true"
  >
    <rect width="32" height="32" rx="9" class="logo-tile" />
    <g :class="turning ? 'logo-nut-turning' : ''">
      <polygon
        points="23.79,20.5 16,25 8.21,20.5 8.21,11.5 16,7 23.79,11.5"
        class="logo-nut"
        stroke-width="1.6"
        stroke-linejoin="round"
      />
      <polygon
        v-if="chamfered"
        points="22.41,19.7 16,23.4 9.59,19.7 9.59,12.3 16,8.6 22.41,12.3"
        class="logo-chamfer"
        stroke-width="0.7"
        stroke-linejoin="round"
      />
    </g>
    <path
      d="M14.4 12.2 18.2 16l-3.8 3.8"
      fill="none"
      class="logo-arrow"
      stroke-width="2.6"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
  </svg>
</template>

<style scoped>
.logo-default {
  --logo-tile: var(--rf-accent);
  --logo-ink: #ffffff;
}

.logo-inverted {
  --logo-tile: #ffffff;
  --logo-ink: var(--color-brand-600);
}

.logo-tile {
  fill: var(--logo-tile);
}

.logo-nut {
  fill: var(--logo-ink);
  stroke: var(--logo-ink);
}

/* Фаска чуть темнее грани: тот же цвет гайки, на треть разбавленный цветом плитки */
.logo-chamfer {
  fill: none;
  stroke: color-mix(in oklab, var(--logo-ink) 70%, var(--logo-tile));
}

/* Стрелка — «вырез» в гайке: цветом плитки, а не прозрачностью, чтобы не зависеть от фона. */
.logo-arrow {
  stroke: var(--logo-tile);
}

.logo-nut-turning {
  transform-origin: 16px 16px;
  animation: nut-turn 1.1s cubic-bezier(0.6, 0, 0.2, 1) infinite;
}

@keyframes nut-turn {
  0%,
  30% {
    transform: rotate(0deg);
  }

  70%,
  100% {
    transform: rotate(60deg);
  }
}
</style>
