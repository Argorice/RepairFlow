<script setup lang="ts">
import { Monitor, Moon, Sun } from '@lucide/vue'
import { computed } from 'vue'
import { locale, setLocale, t } from '@/i18n'
import { setTheme, themePreference, type ThemePreference } from '@/lib/theme'

/**
 * Компактные переключатели языка и темы — для бокового меню и экрана входа.
 * Полные настройки с подписями живут в профиле. На синей подложке экрана входа — белый вариант.
 */
const props = withDefaults(defineProps<{ inverted?: boolean }>(), { inverted: false })

const tones = computed(() =>
  props.inverted
    ? { button: 'text-white/80 hover:bg-white/10 hover:text-white', on: 'text-white', off: 'text-white/50' }
    : { button: 'text-ink-2 hover:bg-surface-2 hover:text-ink', on: 'text-ink', off: 'text-ink-3' },
)

const ORDER: ThemePreference[] = ['system', 'light', 'dark']

const themeIcon = computed(
  () => ({ system: Monitor, light: Sun, dark: Moon })[themePreference.value],
)

function cycleTheme(): void {
  const index = ORDER.indexOf(themePreference.value)
  setTheme(ORDER[(index + 1) % ORDER.length] ?? 'system')
}
</script>

<template>
  <div class="flex items-center gap-1">
    <button
      type="button"
      class="pressable inline-flex h-9 items-center gap-1 rounded-lg px-2.5 text-xs font-semibold tracking-wide"
      :class="tones.button"
      :aria-label="t('settings.language')"
      @click="setLocale(locale === 'en' ? 'ru' : 'en')"
    >
      <span :class="locale === 'en' ? tones.on : tones.off">EN</span>
      <span :class="tones.off" aria-hidden="true">/</span>
      <span :class="locale === 'ru' ? tones.on : tones.off">RU</span>
    </button>

    <button
      type="button"
      class="pressable inline-flex size-9 items-center justify-center rounded-lg"
      :class="tones.button"
      :aria-label="`${t('settings.theme')}: ${t(`settings.themes.${themePreference}`)}`"
      :title="`${t('settings.theme')}: ${t(`settings.themes.${themePreference}`)}`"
      @click="cycleTheme"
    >
      <component :is="themeIcon" class="size-[18px]" aria-hidden="true" />
    </button>
  </div>
</template>
