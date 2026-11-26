/**
 * localStorage, который не роняет приложение. В приватном режиме Safari, при запрете кук
 * или в встроенных браузерах доступ к хранилищу бросает исключение — настройки тогда
 * просто не запоминаются, а интерфейс работает как обычно.
 */
export function readSetting(key: string): string | null {
  try {
    return window.localStorage.getItem(key)
  } catch {
    return null
  }
}

export function writeSetting(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value)
  } catch {
    // Не запомнили — не беда, выбор действует до перезагрузки.
  }
}
