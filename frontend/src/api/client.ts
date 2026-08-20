import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios'
import type { AuthResponse, ProblemDetails } from './types'

/**
 * Access-токен живёт только в памяти вкладки: в localStorage его класть нельзя —
 * оттуда его достанет любой XSS. Долгоживущий refresh лежит в httpOnly-куке,
 * поэтому после перезагрузки страницы сессия восстанавливается запросом /auth/refresh.
 */
let accessToken: string | null = null

export function setAccessToken(token: string | null): void {
  accessToken = token
}

export function getAccessToken(): string | null {
  return accessToken
}

export const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

/** Ошибка API с разобранным ProblemDetails: сообщение уже готово для показа человеку. */
export class ApiError extends Error {
  readonly status: number
  readonly errors: Record<string, string[]>

  constructor(message: string, status: number, errors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }

  /** Первая ошибка по конкретному полю формы. */
  fieldError(field: string): string | undefined {
    const key = Object.keys(this.errors).find((k) => k.toLowerCase() === field.toLowerCase())
    return key ? this.errors[key]?.[0] : undefined
  }
}

export const http = axios.create({
  baseURL: apiBaseUrl,
  // Нужно, чтобы браузер отправлял httpOnly-куку с refresh-токеном.
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})

type RetriableConfig = AxiosRequestConfig & { _retried?: boolean }

/** Пока идёт обновление токена, параллельные 401 ждут один и тот же запрос, а не плодят свои. */
let refreshPromise: Promise<string | null> | null = null

/** Колбэк, который вызывается, когда сессию восстановить не удалось. Ставит его стор авторизации. */
let onSessionExpired: (() => void) | null = null

export function setSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler
}

export async function refreshSession(): Promise<string | null> {
  refreshPromise ??= (async () => {
    try {
      const { data } = await axios.post<AuthResponse>(
        '/api/auth/refresh',
        {},
        { baseURL: apiBaseUrl, withCredentials: true },
      )

      setAccessToken(data.accessToken)
      return data.accessToken
    } catch {
      setAccessToken(null)
      return null
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const config = error.config as RetriableConfig | undefined
    const status = error.response?.status ?? 0

    const isAuthCall = typeof config?.url === 'string' && config.url.includes('/api/auth/')

    // Тихое обновление: словили 401 → обновили пару токенов → повторили исходный запрос.
    // Пользователь ничего не замечает, форма не «выкидывает» его на страницу входа.
    if (status === 401 && config && !config._retried && !isAuthCall) {
      config._retried = true

      const token = await refreshSession()

      if (token) {
        return http.request(config)
      }

      onSessionExpired?.()
    }

    throw toApiError(error)
  },
)

function toApiError(error: AxiosError<ProblemDetails>): ApiError {
  const status = error.response?.status ?? 0
  const problem = error.response?.data

  if (!error.response) {
    return new ApiError('Сервер недоступен. Проверьте подключение к сети.', 0)
  }

  const message =
    problem?.detail?.trim() ||
    problem?.title?.trim() ||
    defaultMessageFor(status)

  return new ApiError(message, status, problem?.errors ?? {})
}

function defaultMessageFor(status: number): string {
  switch (status) {
    case 400:
      return 'Запрос не прошёл проверку.'
    case 401:
      return 'Нужно войти заново.'
    case 403:
      return 'Недостаточно прав для этого действия.'
    case 404:
      return 'Запись не найдена.'
    case 409:
      return 'Действие сейчас недопустимо.'
    case 413:
      return 'Файл слишком большой.'
    default:
      return 'Что-то пошло не так. Попробуйте ещё раз.'
  }
}
