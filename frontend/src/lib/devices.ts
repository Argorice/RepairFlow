import { t } from '@/i18n'

/**
 * Тип устройства приходит с сервера кодом: «laptop», а не «Laptop» или «Ноутбук».
 * Слово подставляет интерфейс — на языке того, кто смотрит, а не того, кто создал заявку.
 * Устройство, которого нет в списке, хранится свободным текстом и показывается как есть.
 */
export const DEVICE_TYPES = ['laptop', 'phone', 'tablet', 'tv', 'monitor', 'console', 'printer'] as const

export type DeviceType = (typeof DEVICE_TYPES)[number]

export function isKnownDeviceType(value: string): value is DeviceType {
  return (DEVICE_TYPES as readonly string[]).includes(value)
}

export function deviceTypeLabel(value: string): string {
  return isKnownDeviceType(value) ? t(`devices.${value}`) : value
}
