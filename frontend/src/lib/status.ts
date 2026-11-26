import { OrderItemType, OrderPriority, OrderStatus, UserRole } from '@/api/types'
import { t } from '@/i18n'

/**
 * Тон — смысловой цвет статуса. Классы собраны целиком, а не склеены из кусочков:
 * Tailwind находит утилиты по полному имени в исходниках, `bg-${tone}-soft` он бы не увидел.
 */
export type Tone = 'neutral' | 'info' | 'warn' | 'accent' | 'success' | 'done' | 'danger'

interface ToneClasses {
  /** Бейдж: мягкая заливка, тёмный текст, тонкая рамка. */
  badge: string
  /** Заливка точки и пройденного шага шкалы. */
  solid: string
  /** Цвет текста для акцентов. */
  text: string
  /** Мягкая заливка без текста — фон иконки, плашки. */
  soft: string
}

const TONES: Record<Tone, ToneClasses> = {
  neutral: {
    badge: 'bg-neutral-soft text-neutral-ink ring-neutral-line',
    solid: 'bg-neutral-solid',
    text: 'text-neutral-ink',
    soft: 'bg-neutral-soft',
  },
  info: {
    badge: 'bg-info-soft text-info-ink ring-info-line',
    solid: 'bg-info-solid',
    text: 'text-info-ink',
    soft: 'bg-info-soft',
  },
  warn: {
    badge: 'bg-warn-soft text-warn-ink ring-warn-line',
    solid: 'bg-warn-solid',
    text: 'text-warn-ink',
    soft: 'bg-warn-soft',
  },
  accent: {
    badge: 'bg-accent-soft text-accent-ink ring-accent-line',
    solid: 'bg-accent',
    text: 'text-accent-ink',
    soft: 'bg-accent-soft',
  },
  success: {
    badge: 'bg-success-soft text-success-ink ring-success-line',
    solid: 'bg-success-solid',
    text: 'text-success-ink',
    soft: 'bg-success-soft',
  },
  done: {
    badge: 'bg-done-soft text-done-ink ring-done-line',
    solid: 'bg-done-solid',
    text: 'text-done-ink',
    soft: 'bg-done-soft',
  },
  danger: {
    badge: 'bg-danger-soft text-danger-ink ring-danger-line',
    solid: 'bg-danger-solid',
    text: 'text-danger-ink',
    soft: 'bg-danger-soft',
  },
}

const STATUS_TONES: Record<OrderStatus, Tone> = {
  [OrderStatus.New]: 'neutral',
  [OrderStatus.Diagnostics]: 'info',
  [OrderStatus.AwaitingEstimateApproval]: 'warn',
  [OrderStatus.InProgress]: 'accent',
  [OrderStatus.ReadyForPickup]: 'success',
  [OrderStatus.Completed]: 'done',
  [OrderStatus.Cancelled]: 'neutral',
  [OrderStatus.ClientRejected]: 'danger',
}

export function toneClasses(tone: Tone): ToneClasses {
  return TONES[tone]
}

export function statusTone(status: OrderStatus): Tone {
  return STATUS_TONES[status] ?? 'neutral'
}

export function statusStyle(status: OrderStatus): ToneClasses {
  return TONES[statusTone(status)]
}

/** Основной маршрут заявки — по нему рисуется шкала в карточке. */
export const MAIN_PATH: OrderStatus[] = [
  OrderStatus.New,
  OrderStatus.Diagnostics,
  OrderStatus.AwaitingEstimateApproval,
  OrderStatus.InProgress,
  OrderStatus.ReadyForPickup,
  OrderStatus.Completed,
]

export const TERMINAL_STATUSES: OrderStatus[] = [OrderStatus.Cancelled, OrderStatus.ClientRejected]

export const ALL_STATUSES = Object.values(OrderStatus)

/**
 * Подписи строятся на клиенте из enum, а не берутся из ответа сервера: так язык интерфейса
 * переключается мгновенно, без перезапроса данных.
 */
export const statusLabel = (status: OrderStatus): string => t(`status.${status}`)

export const priorityLabel = (priority: OrderPriority): string => t(`priority.${priority}`)

export const roleLabel = (role: UserRole): string => t(`role.${role}`)

export const itemTypeLabel = (type: OrderItemType): string => t(`itemType.${type}`)

/**
 * Кнопка перехода называет действие, а не статус: «Начать диагностику» вместо «Диагностика».
 * Возврат сметы на доработку — единственный переход, которому нужно знать и откуда идём.
 */
export function transitionLabel(from: OrderStatus, to: OrderStatus): string {
  if (from === OrderStatus.AwaitingEstimateApproval && to === OrderStatus.Diagnostics) {
    return t('transition.reviseEstimate')
  }

  return t(`transition.${to}`)
}

/** Отмена и отказ — действия «назад», они не должны спорить с основной кнопкой. */
export function isDestructiveTransition(to: OrderStatus): boolean {
  return to === OrderStatus.Cancelled || to === OrderStatus.ClientRejected
}
