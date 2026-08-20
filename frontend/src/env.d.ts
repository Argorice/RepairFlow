/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Адрес API. По умолчанию пусто — API живёт на том же origin (прокси Vite или rewrite на Vercel). */
  readonly VITE_API_URL?: string
  /** Адрес хаба SignalR. Прямой адрес бэкенда даёт настоящий WebSocket вместо long polling. */
  readonly VITE_REALTIME_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
