/**
 * Контракты бэкенда. Enum'ы приходят строками (на сервере стоит JsonStringEnumConverter),
 * поэтому здесь они описаны через const-объект и union — TS-enum в проекте запрещён
 * настройкой erasableSyntaxOnly.
 */

export const OrderStatus = {
  New: 'New',
  Diagnostics: 'Diagnostics',
  AwaitingEstimateApproval: 'AwaitingEstimateApproval',
  InProgress: 'InProgress',
  ReadyForPickup: 'ReadyForPickup',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
  ClientRejected: 'ClientRejected',
} as const

export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus]

export const OrderPriority = {
  Low: 'Low',
  Normal: 'Normal',
  High: 'High',
} as const

export type OrderPriority = (typeof OrderPriority)[keyof typeof OrderPriority]

export const UserRole = {
  Client: 'Client',
  Technician: 'Technician',
  Manager: 'Manager',
} as const

export type UserRole = (typeof UserRole)[keyof typeof UserRole]

export const OrderItemType = {
  Part: 'Part',
  Labor: 'Labor',
} as const

export type OrderItemType = (typeof OrderItemType)[keyof typeof OrderItemType]

export const OrderEventKind = {
  StatusChanged: 'StatusChanged',
  TechnicianAssigned: 'TechnicianAssigned',
  CommentAdded: 'CommentAdded',
} as const

export type OrderEventKind = (typeof OrderEventKind)[keyof typeof OrderEventKind]

export interface UserDto {
  id: string
  email: string
  fullName: string
  phone: string | null
  role: UserRole
  roleLabel: string
  isActive: boolean
  createdAt: string
}

export interface UserSummaryDto {
  id: string
  fullName: string
  email: string
  role: UserRole
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: UserDto
}

export interface StatusOptionDto {
  status: OrderStatus
  label: string
}

export interface OrderListItemDto {
  id: string
  number: string
  deviceType: string
  brand: string
  model: string
  status: OrderStatus
  statusLabel: string
  priority: OrderPriority
  client: UserSummaryDto
  technician: UserSummaryDto | null
  estimatedCost: number | null
  finalCost: number | null
  createdAt: string
  updatedAt: string
  completedAt: string | null
}

export interface OrderDetailsDto {
  id: string
  number: string
  deviceType: string
  brand: string
  model: string
  serialNumber: string | null
  problemDescription: string
  status: OrderStatus
  statusLabel: string
  priority: OrderPriority
  client: UserSummaryDto
  technician: UserSummaryDto | null
  estimatedCost: number | null
  finalCost: number | null
  estimateTotal: number
  itemsCount: number
  commentsCount: number
  attachmentsCount: number
  createdAt: string
  updatedAt: string
  completedAt: string | null
  availableTransitions: StatusOptionDto[]
  canEdit: boolean
  canManageEstimate: boolean
}

export interface OrderStatusHistoryDto {
  id: string
  fromStatus: OrderStatus | null
  fromStatusLabel: string | null
  toStatus: OrderStatus
  toStatusLabel: string
  changedBy: UserSummaryDto
  comment: string | null
  changedAt: string
}

export interface OrderItemDto {
  id: string
  type: OrderItemType
  typeLabel: string
  name: string
  quantity: number
  unitPrice: number
  total: number
}

export interface EstimateDto {
  items: OrderItemDto[]
  partsTotal: number
  laborTotal: number
  total: number
  isEditable: boolean
  awaitingApproval: boolean
}

export interface CommentDto {
  id: string
  text: string
  isInternal: boolean
  author: UserSummaryDto
  createdAt: string
}

export interface AttachmentDto {
  id: string
  orderId: string
  fileName: string
  contentType: string
  sizeBytes: number
  isImage: boolean
  uploadedBy: UserSummaryDto
  uploadedAt: string
  url: string
}

export interface StatusCountDto {
  status: OrderStatus
  label: string
  count: number
}

export interface DailyCountDto {
  date: string
  created: number
  completed: number
}

export interface TechnicianLoadDto {
  technicianId: string
  fullName: string
  activeOrders: number
  completedInPeriod: number
  revenueInPeriod: number
}

export interface DashboardSummaryDto {
  from: string
  to: string
  totalOrders: number
  openOrders: number
  overdueEstimates: number
  completedInPeriod: number
  revenueInPeriod: number
  averageRepairHours: number | null
  byStatus: StatusCountDto[]
  daily: DailyCountDto[]
  technicianLoad: TechnicianLoadDto[]
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

export interface OrderQuery {
  status?: OrderStatus
  priority?: OrderPriority
  technicianId?: string
  clientId?: string
  search?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
  sort?: string
}

export interface OrderEventDto {
  orderId: string
  number: string
  kind: OrderEventKind
  status: OrderStatus
  statusLabel: string
  message: string | null
  at: string
}

/** Формат ошибок бэкенда — ProblemDetails по RFC 7807. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}
