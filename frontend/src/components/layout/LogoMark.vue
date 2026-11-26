<script setup lang="ts">
/**
 * Знак RepairFlow: гайка — ремонт, шеврон внутри — движение заявки вперёд по шагам.
 * Нарисован для маленьких размеров: на 16 px вкладки браузера читаются и гайка, и стрелка.
 *
 * `turning` — гайка проворачивается рывками, как под ключом с трещоткой. Это вся анимация
 * загрузочного экрана: шестиугольник симметричен на 60°, поэтому цикл бесшовный.
 */
withDefaults(defineProps<{ size?: number; inverted?: boolean; turning?: boolean }>(), {
  size: 32,
  inverted: false,
  turning: false,
})
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
