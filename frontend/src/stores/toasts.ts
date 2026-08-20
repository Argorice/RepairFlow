import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastKind = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  kind: ToastKind
  text: string
}

let nextId = 1

export const useToastStore = defineStore('toasts', () => {
  const items = ref<Toast[]>([])

  function push(kind: ToastKind, text: string, timeout = 4000): void {
    const toast: Toast = { id: nextId++, kind, text }
    items.value.push(toast)

    window.setTimeout(() => remove(toast.id), timeout)
  }

  function remove(id: number): void {
    items.value = items.value.filter((toast) => toast.id !== id)
  }

  const success = (text: string) => push('success', text)
  const error = (text: string) => push('error', text, 6000)
  const info = (text: string) => push('info', text)

  return { items, push, remove, success, error, info }
})
