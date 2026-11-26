import { intlLocale, t } from '@/i18n'

/** Валюта сервисного центра. Меняется в одном месте — от языка интерфейса она не зависит. */
const CURRENCY = 'USD'

/**
 * Форматтеры Intl дорого создавать на каждый вызов, поэтому они кешируются — но по локали:
 * после переключения языка нужны новые, иначе даты остались бы в прежнем формате.
 */
const cache = new Map<string, Intl.NumberFormat | Intl.DateTimeFormat | Intl.RelativeTimeFormat>()

function cached<T extends Intl.NumberFormat | Intl.DateTimeFormat | Intl.RelativeTimeFormat>(
  name: string,
  create: (locale: string) => T,
): T {
  const key = `${intlLocale.value}:${name}`
  let formatter = cache.get(key) as T | undefined

  if (!formatter) {
    formatter = create(intlLocale.value)
    cache.set(key, formatter)
  }

  return formatter
}

const money = () =>
  cached('money', (locale) =>
    new Intl.NumberFormat(locale, { style: 'currency', currency: CURRENCY, maximumFractionDigits: 0 }),
  )

const moneyPrecise = () =>
  cached('money-precise', (locale) =>
    new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: CURRENCY,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }),
  )

const dateTime = () =>
  cached('date-time', (locale) =>
    new Intl.DateTimeFormat(locale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }),
  )

const dateShort = () =>
  cached('date-short', (locale) => new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }))

const dateLong = () =>
  cached('date-long', (locale) =>
    new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric' }),
  )

const relative = () =>
  cached('relative', (locale) => new Intl.RelativeTimeFormat(locale, { numeric: 'auto' }))

const decimal = () =>
  cached('decimal', (locale) => new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }))

export function formatMoney(value: number | null | undefined, precise = false): string {
  if (value === null || value === undefined) {
    return '—'
  }

  return precise ? moneyPrecise().format(value) : money().format(value)
}

/** Количество в смете: «1,5» по-русски и «1.5» по-английски. */
export function formatNumber(value: number): string {
  return cached('number', (locale) => new Intl.NumberFormat(locale, { maximumFractionDigits: 2 })).format(value)
}

export function formatDateTime(value: string | null | undefined): string {
  return value ? dateTime().format(new Date(value)) : '—'
}

export function formatDay(value: string | null | undefined): string {
  return value ? dateShort().format(new Date(value)) : '—'
}

export function formatDate(value: string | null | undefined): string {
  return value ? dateLong().format(new Date(value)) : '—'
}

/** «5 минут назад» читается быстрее полной даты там, где важна свежесть. */
export function formatRelative(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }

  const diffMs = new Date(value).getTime() - Date.now()
  const minutes = Math.round(diffMs / 60_000)

  if (Math.abs(minutes) < 1) {
    return t('time.justNow')
  }

  if (Math.abs(minutes) < 60) {
    return relative().format(minutes, 'minute')
  }

  const hours = Math.round(minutes / 60)
  if (Math.abs(hours) < 24) {
    return relative().format(hours, 'hour')
  }

  const days = Math.round(hours / 24)
  if (Math.abs(days) < 30) {
    return relative().format(days, 'day')
  }

  return formatDay(value)
}

export function formatHours(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '—'
  }

  return value < 24
    ? t('time.hoursShort', { value: decimal().format(value) })
    : t('time.daysShort', { value: decimal().format(value / 24) })
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return t('size.bytes', { value: bytes })
  }

  if (bytes < 1024 * 1024) {
    return t('size.kilobytes', { value: Math.round(bytes / 1024) })
  }

  return t('size.megabytes', { value: decimal().format(bytes / 1024 / 1024) })
}

export function initials(fullName: string): string {
  return fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

/** Дата для <input type="date">: локальная, а не UTC, иначе вечером «сегодня» превращается во «вчера». */
export function toDateInput(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')

  return `${year}-${month}-${day}`
}
