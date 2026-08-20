import { OrderPriority, OrderStatus, UserRole } from '@/api/types'

interface StatusStyle {
  /** Классы бейджа: мягкая заливка, тёмный текст, тонкая рамка. */
  badge: string
  /** Заливка точки и пройденного шага временной шкалы. */
  solid: string
  /** Цвет текста для акцентов. */
  text: string
}

const STATUS_STYLES: Record<OrderStatus, StatusStyle> = {
  [OrderStatus.New]: {
    badge: 'bg-slate-100 text-slate-700 ring-slate-200',
    solid: 'bg-slate-500',
    text: 'text-slate-600',
  },
  [OrderStatus.Diagnostics]: {
    badge: 'bg-sky-50 text-sky-700 ring-sky-200',
    solid: 'bg-sky-500',
    text: 'text-sky-600',
  },
  [OrderStatus.AwaitingEstimateApproval]: {
    badge: 'bg-amber-50 text-amber-700 ring-amber-200',
    solid: 'bg-amber-500',
    text: 'text-amber-600',
  },
  [OrderStatus.InProgress]: {
    badge: 'bg-brand-50 text-brand-700 ring-brand-200',
    solid: 'bg-brand-600',
    text: 'text-brand-600',
  },
  [OrderStatus.ReadyForPickup]: {
    badge: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
    solid: 'bg-emerald-500',
    text: 'text-emerald-600',
  },
  [OrderStatus.Completed]: {
    badge: 'bg-emerald-100 text-emerald-800 ring-emerald-300',
    solid: 'bg-emerald-700',
    text: 'text-emerald-700',
  },
  [OrderStatus.Cancelled]: {
    badge: 'bg-slate-100 text-slate-500 ring-slate-200',
    solid: 'bg-slate-400',
    text: 'text-slate-500',
  },
  [OrderStatus.ClientRejected]: {
    badge: 'bg-rose-50 text-rose-700 ring-rose-200',
    solid: 'bg-rose-500',
    text: 'text-rose-600',
  },
}

export function statusStyle(status: OrderStatus): StatusStyle {
  return STATUS_STYLES[status] ?? STATUS_STYLES[OrderStatus.New]
}

/** Основной маршрут заявки — по нему рисуется временная шкала в карточке. */
export const MAIN_PATH: OrderStatus[] = [
  OrderStatus.New,
  OrderStatus.Diagnostics,
  OrderStatus.AwaitingEstimateApproval,
  OrderStatus.InProgress,
  OrderStatus.ReadyForPickup,
  OrderStatus.Completed,
]

export const TERMINAL_STATUSES: OrderStatus[] = [OrderStatus.Cancelled, OrderStatus.ClientRejected]

export const STATUS_LABELS: Record<OrderStatus, string> = {
  [OrderStatus.New]: 'Новая',
  [OrderStatus.Diagnostics]: 'Диагностика',
  [OrderStatus.AwaitingEstimateApproval]: 'Ожидает сметы',
  [OrderStatus.InProgress]: 'В работе',
  [OrderStatus.ReadyForPickup]: 'Готова к выдаче',
  [OrderStatus.Completed]: 'Выдана',
  [OrderStatus.Cancelled]: 'Отменена',
  [OrderStatus.ClientRejected]: 'Отказ клиента',
}

export const PRIORITY_LABELS: Record<OrderPriority, string> = {
  [OrderPriority.Low]: 'Низкий',
  [OrderPriority.Normal]: 'Обычный',
  [OrderPriority.High]: 'Высокий',
}

export const PRIORITY_STYLES: Record<OrderPriority, string> = {
  [OrderPriority.Low]: 'text-slate-400',
  [OrderPriority.Normal]: 'text-slate-400',
  [OrderPriority.High]: 'text-rose-500',
}

export const ROLE_LABELS: Record<UserRole, string> = {
  [UserRole.Client]: 'Клиент',
  [UserRole.Technician]: 'Мастер',
  [UserRole.Manager]: 'Менеджер',
}
