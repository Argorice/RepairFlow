/** Валюта сервисного центра. Меняется в одном месте. */
const CURRENCY = 'RUB'

const money = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: CURRENCY,
  maximumFractionDigits: 0,
})

const moneyPrecise = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: CURRENCY,
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const dateTime = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const dateOnly = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: 'short',
})

const relative = new Intl.RelativeTimeFormat('ru-RU', { numeric: 'auto' })

export function formatMoney(value: number | null | undefined, precise = false): string {
  if (value === null || value === undefined) {
    return '—'
  }

  return precise ? moneyPrecise.format(value) : money.format(value)
}

export function formatDateTime(value: string | null | undefined): string {
  return value ? dateTime.format(new Date(value)) : '—'
}

export function formatDay(value: string | null | undefined): string {
  return value ? dateOnly.format(new Date(value)) : '—'
}

/** «5 минут назад» — читается быстрее, чем полная дата, там где важна свежесть. */
export function formatRelative(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }

  const diffMs = new Date(value).getTime() - Date.now()
  const minutes = Math.round(diffMs / 60_000)

  if (Math.abs(minutes) < 60) {
    return relative.format(minutes, 'minute')
  }

  const hours = Math.round(minutes / 60)
  if (Math.abs(hours) < 24) {
    return relative.format(hours, 'hour')
  }

  return relative.format(Math.round(hours / 24), 'day')
}

export function formatHours(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '—'
  }

  if (value < 24) {
    return `${value.toFixed(1)} ч`
  }

  return `${(value / 24).toFixed(1)} дн`
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} Б`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(0)} КБ`
  }

  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`
}

export function initials(fullName: string): string {
  return fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}
