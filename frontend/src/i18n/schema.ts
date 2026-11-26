import type { en } from './en'

/**
 * Формы множественного числа по CLDR. `other` обязательна — она же запасная.
 * Поэтому ключ `other` в обычных разделах словаря занимать нельзя: раздел с ним
 * типы примут за набор форм.
 */
export type PluralForms = Partial<Record<Intl.LDMLPluralRule, string>> & { other: string }

/** Помечает запись словаря как множественную. Возвращает объект как есть — это только тип. */
export function plural<T extends PluralForms>(forms: T): T {
  return forms
}

/**
 * Форма словаря выводится из английского: каждый лист — строка или набор форм,
 * но у русского свой набор форм, поэтому сами формы не сравниваются, только ключи.
 */
type Shape<T> = T extends string
  ? string
  : T extends PluralForms
    ? PluralForms
    : { [K in keyof T]: Shape<T[K]> }

export type Messages = Shape<typeof en>

type Leaf = string | PluralForms

/** Все ключи словаря в виде путей через точку: 'orders.title', 'status.InProgress'… */
type Paths<T, Prefix extends string = ''> = {
  [K in keyof T & string]: T[K] extends Leaf ? `${Prefix}${K}` : Paths<T[K], `${Prefix}${K}.`>
}[keyof T & string]

export type MessageKey = Paths<typeof en>
