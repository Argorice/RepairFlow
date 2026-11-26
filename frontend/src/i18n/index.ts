import { computed, ref } from 'vue'
import { readSetting, writeSetting } from '@/lib/storage'
import { en } from './en'
import { ru } from './ru'
import type { MessageKey, Messages, PluralForms } from './schema'

/**
 * Локализация без библиотеки: двух языков и сотни-другой строк хватает на 80 строк кода,
 * а типы получаются строже, чем у vue-i18n из коробки. Ключ — путь в английском словаре,
 * поэтому опечатка в ключе или забытый перевод в ru.ts — это ошибка компиляции, а не пустая
 * строка на экране.
 */
export type Locale = 'en' | 'ru'

export const LOCALES: readonly Locale[] = ['en', 'ru']

const STORAGE_KEY = 'rf.locale'

const dictionaries: Record<Locale, Messages> = { en, ru }

function initialLocale(): Locale {
  const stored = readSetting(STORAGE_KEY)
  // По умолчанию — английский: витрина рассчитана на англоязычного заказчика.
  return stored === 'ru' ? 'ru' : 'en'
}

export const locale = ref<Locale>(initialLocale())

/** Тег для Intl: форматы денег, дат и множественного числа берутся у браузера, а не пишутся руками. */
export const intlLocale = computed(() => (locale.value === 'ru' ? 'ru-RU' : 'en-US'))

const pluralRules = computed(() => new Intl.PluralRules(intlLocale.value))

export function setLocale(next: Locale): void {
  locale.value = next
  writeSetting(STORAGE_KEY, next)
  document.documentElement.lang = next
}

type Params = Record<string, string | number>

function lookup(messages: Messages, key: string): string | PluralForms | undefined {
  let node: unknown = messages

  for (const part of key.split('.')) {
    if (node === null || typeof node !== 'object') {
      return undefined
    }

    node = (node as Record<string, unknown>)[part]
  }

  return typeof node === 'string' || (node !== null && typeof node === 'object' && 'other' in node)
    ? (node as string | PluralForms)
    : undefined
}

function interpolate(template: string, params?: Params): string {
  if (!params) {
    return template
  }

  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? String(params[name]) : match,
  )
}

/**
 * Перевод по ключу. Для множественного числа передайте `count`: русскому нужны формы
 * one/few/many («1 заявка», «3 заявки», «5 заявок»), английскому хватает one/other —
 * какую взять, решает Intl.PluralRules.
 *
 * Функция читает `locale.value`, поэтому шаблон, который её вызвал, перерисуется сам
 * при смене языка.
 */
export function t(key: MessageKey, params?: Params): string {
  const entry = lookup(dictionaries[locale.value], key) ?? lookup(en, key)

  if (entry === undefined) {
    return key
  }

  if (typeof entry === 'string') {
    return interpolate(entry, params)
  }

  const count = Number(params?.count ?? 0)
  const form = pluralRules.value.select(count)

  return interpolate(entry[form] ?? entry.other, params)
}

export type { MessageKey }

document.documentElement.lang = locale.value
