import { http } from './client'
import type {
  AttachmentDto,
  AuthResponse,
  CommentDto,
  DashboardSummaryDto,
  EstimateDto,
  OrderDetailsDto,
  OrderItemDto,
  OrderItemType,
  OrderListItemDto,
  OrderPriority,
  OrderQuery,
  OrderStatus,
  OrderStatusHistoryDto,
  PagedResult,
  UserDto,
  UserRole,
} from './types'

export const authApi = {
  async login(email: string, password: string): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/api/auth/login', { email, password })
    return data
  },

  async register(payload: {
    email: string
    password: string
    fullName: string
    phone?: string | null
  }): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/api/auth/register', payload)
    return data
  },

  /** Вход одной кнопкой — то, ради чего заказчик не закрывает вкладку. */
  async demo(role: UserRole): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/api/auth/demo', { role })
    return data
  },

  async me(): Promise<UserDto> {
    const { data } = await http.get<UserDto>('/api/auth/me')
    return data
  },

  async logout(): Promise<void> {
    await http.post('/api/auth/logout')
  },
}

export const ordersApi = {
  async list(query: OrderQuery): Promise<PagedResult<OrderListItemDto>> {
    const { data } = await http.get<PagedResult<OrderListItemDto>>('/api/orders', { params: query })
    return data
  },

  async byId(id: string): Promise<OrderDetailsDto> {
    const { data } = await http.get<OrderDetailsDto>(`/api/orders/${id}`)
    return data
  },

  async create(payload: {
    deviceType: string
    brand: string
    model: string
    serialNumber?: string | null
    problemDescription: string
    priority?: OrderPriority | null
  }): Promise<OrderDetailsDto> {
    const { data } = await http.post<OrderDetailsDto>('/api/orders', payload)
    return data
  },

  async update(id: string, payload: Record<string, unknown>): Promise<OrderDetailsDto> {
    const { data } = await http.patch<OrderDetailsDto>(`/api/orders/${id}`, payload)
    return data
  },

  async changeStatus(id: string, status: OrderStatus, comment?: string): Promise<OrderDetailsDto> {
    const { data } = await http.post<OrderDetailsDto>(`/api/orders/${id}/status`, { status, comment })
    return data
  },

  async assign(id: string, technicianId: string | null): Promise<OrderDetailsDto> {
    const { data } = await http.post<OrderDetailsDto>(`/api/orders/${id}/assign`, { technicianId })
    return data
  },

  async history(id: string): Promise<OrderStatusHistoryDto[]> {
    const { data } = await http.get<OrderStatusHistoryDto[]>(`/api/orders/${id}/history`)
    return data
  },
}

export const estimateApi = {
  async get(orderId: string): Promise<EstimateDto> {
    const { data } = await http.get<EstimateDto>(`/api/orders/${orderId}/items`)
    return data
  },

  async add(
    orderId: string,
    payload: { type: OrderItemType; name: string; quantity: number; unitPrice: number },
  ): Promise<OrderItemDto> {
    const { data } = await http.post<OrderItemDto>(`/api/orders/${orderId}/items`, payload)
    return data
  },

  async update(
    orderId: string,
    itemId: string,
    payload: { type: OrderItemType; name: string; quantity: number; unitPrice: number },
  ): Promise<OrderItemDto> {
    const { data } = await http.put<OrderItemDto>(`/api/orders/${orderId}/items/${itemId}`, payload)
    return data
  },

  async remove(orderId: string, itemId: string): Promise<void> {
    await http.delete(`/api/orders/${orderId}/items/${itemId}`)
  },

  async approve(orderId: string): Promise<OrderDetailsDto> {
    const { data } = await http.post<OrderDetailsDto>(`/api/orders/${orderId}/estimate/approve`)
    return data
  },

  async reject(orderId: string, reason?: string): Promise<OrderDetailsDto> {
    const { data } = await http.post<OrderDetailsDto>(`/api/orders/${orderId}/estimate/reject`, { reason })
    return data
  },
}

export const commentsApi = {
  async list(orderId: string): Promise<CommentDto[]> {
    const { data } = await http.get<CommentDto[]>(`/api/orders/${orderId}/comments`)
    return data
  },

  async add(orderId: string, text: string, isInternal: boolean): Promise<CommentDto> {
    const { data } = await http.post<CommentDto>(`/api/orders/${orderId}/comments`, { text, isInternal })
    return data
  },
}

export const attachmentsApi = {
  async list(orderId: string): Promise<AttachmentDto[]> {
    const { data } = await http.get<AttachmentDto[]>(`/api/orders/${orderId}/attachments`)
    return data
  },

  async upload(orderId: string, file: File): Promise<AttachmentDto> {
    const form = new FormData()
    form.append('file', file)

    const { data } = await http.post<AttachmentDto>(`/api/orders/${orderId}/attachments`, form)
    return data
  },

  async remove(attachmentId: string): Promise<void> {
    await http.delete(`/api/attachments/${attachmentId}`)
  },

  /** Файлы отдаются с проверкой прав, поэтому тянем их с токеном и показываем как blob-ссылку. */
  async blobUrl(attachmentId: string): Promise<string> {
    const { data } = await http.get<Blob>(`/api/attachments/${attachmentId}`, { responseType: 'blob' })
    return URL.createObjectURL(data)
  },
}

export const usersApi = {
  async list(params: { role?: UserRole; search?: string } = {}): Promise<UserDto[]> {
    const { data } = await http.get<UserDto[]>('/api/users', { params })
    return data
  },

  async create(payload: {
    email: string
    password: string
    fullName: string
    phone?: string | null
    role: UserRole
  }): Promise<UserDto> {
    const { data } = await http.post<UserDto>('/api/users', payload)
    return data
  },

  async update(
    id: string,
    payload: { role?: UserRole; isActive?: boolean; fullName?: string; phone?: string | null },
  ): Promise<UserDto> {
    const { data } = await http.patch<UserDto>(`/api/users/${id}`, payload)
    return data
  },

  async updateProfile(payload: { fullName: string; phone?: string | null }): Promise<UserDto> {
    const { data } = await http.put<UserDto>('/api/users/me', payload)
    return data
  },

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await http.post('/api/users/me/password', { currentPassword, newPassword })
  },
}

export const dashboardApi = {
  async summary(params: { from?: string; to?: string } = {}): Promise<DashboardSummaryDto> {
    const { data } = await http.get<DashboardSummaryDto>('/api/dashboard/summary', { params })
    return data
  },
}
