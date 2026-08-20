/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Адрес бэкенда. По умолчанию — локальный http://localhost:5080. */
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
