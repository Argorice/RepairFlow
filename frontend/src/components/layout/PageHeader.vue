<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { appBar } from '@/lib/appBar'

defineProps<{ title: string; description?: string }>()

const heading = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null

onMounted(() => {
  appBar.hasLargeTitle = true
  appBar.largeTitleVisible = true

  if (!heading.value) {
    return
  }

  // Верхняя панель телефона занимает 56px — заголовок «ушёл», когда скрылся под ней.
  observer = new IntersectionObserver(
    ([entry]) => {
      appBar.largeTitleVisible = entry?.isIntersecting ?? false
    },
    { rootMargin: '-56px 0px 0px 0px' },
  )

  observer.observe(heading.value)
})

onBeforeUnmount(() => {
  observer?.disconnect()
  appBar.hasLargeTitle = false
  appBar.largeTitleVisible = false
})
</script>

<template>
  <header class="flex flex-wrap items-end justify-between gap-x-4 gap-y-3">
    <div class="min-w-0">
      <h1 ref="heading" class="font-display text-[28px] leading-tight font-bold lg:text-2xl">
        {{ title }}
      </h1>
      <p v-if="description" class="mt-1 text-sm text-ink-2">{{ description }}</p>
    </div>

    <div v-if="$slots.actions" class="flex flex-wrap items-center gap-2">
      <slot name="actions" />
    </div>
  </header>
</template>
