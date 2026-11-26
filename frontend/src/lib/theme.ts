import { computed, ref } from 'vue'
import { readSetting, writeSetting } from './storage'

export type ThemePreference = 'system' | 'light' | 'dark'
export type ResolvedTheme = 'light' | 'dark'

const STORAGE_KEY = 'rf.theme'

/** Цвет системной панели браузера — совпадает с фоном холста, чтобы шапка не отрывалась от страницы. */
const THEME_COLORS: Record<ResolvedTheme, string> = {
  light: '#f4f5f9',
  dark: '#0b0d13',
}

function readPreference(): ThemePreference {
  const stored = readSetting(STORAGE_KEY)
  return stored === 'light' || stored === 'dark' ? stored : 'system'
}

const media = window.matchMedia('(prefers-color-scheme: dark)')
const systemDark = ref(media.matches)

media.addEventListener('change', (event) => {
  systemDark.value = event.matches
  apply()
})

export const themePreference = ref<ThemePreference>(readPreference())

export const resolvedTheme = computed<ResolvedTheme>(() =>
  themePreference.value === 'system' ? (systemDark.value ? 'dark' : 'light') : themePreference.value,
)

function apply(): void {
  const theme = resolvedTheme.value
  document.documentElement.dataset.theme = theme

  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', THEME_COLORS[theme])
}

export function setTheme(preference: ThemePreference): void {
  themePreference.value = preference
  writeSetting(STORAGE_KEY, preference)
  apply()
}

apply()
