<script setup lang="ts">
import { Gamepad2, Laptop, Monitor, Printer, Smartphone, Tablet, Tv, Wrench } from '@lucide/vue'
import { computed, type Component } from 'vue'

/**
 * Тип устройства — свободный текст на языке клиента, поэтому иконка подбирается
 * по ключевым словам на обоих языках. Не нашлось — нейтральный гаечный ключ.
 */
const props = withDefaults(defineProps<{ type: string; size?: 'sm' | 'md' }>(), { size: 'md' })

const RULES: [RegExp, Component][] = [
  [/laptop|notebook|macbook|ноутбук/i, Laptop],
  [/phone|iphone|смартфон|телефон/i, Smartphone],
  [/tablet|ipad|планшет/i, Tablet],
  [/\btv\b|television|телевизор/i, Tv],
  [/monitor|display|монитор/i, Monitor],
  [/printer|принтер|мфу/i, Printer],
  [/console|playstation|xbox|switch|консоль|приставка/i, Gamepad2],
]

const icon = computed(() => RULES.find(([pattern]) => pattern.test(props.type))?.[1] ?? Wrench)
</script>

<template>
  <span
    class="flex shrink-0 items-center justify-center rounded-xl bg-surface-2 text-ink-2"
    :class="size === 'sm' ? 'size-9' : 'size-11'"
    aria-hidden="true"
  >
    <component :is="icon" :class="size === 'sm' ? 'size-[18px]' : 'size-5'" />
  </span>
</template>
