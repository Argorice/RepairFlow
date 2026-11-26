<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import LogoMark from '@/components/layout/LogoMark.vue'
import AppToasts from '@/components/ui/AppToasts.vue'
import { locale, t } from '@/i18n'
import { pageTransition, updateDocumentTitle } from '@/router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()

const withShell = computed(() => auth.isAuthenticated && route.meta.public !== true)

/**
 * Демо живёт на бесплатном хостинге, который усыпляет сервис без трафика.
 * Молчаливая заставка в такой момент выглядит как зависший сайт, поэтому через три секунды
 * объясняем, чего ждём. Само ожидание ограничено таймаутом — вечно висеть здесь нельзя.
 */
const slow = ref(false)
let timer: number | undefined

onMounted(() => {
  timer = window.setTimeout(() => (slow.value = true), 3000)
})

onBeforeUnmount(() => window.clearTimeout(timer))

watch(locale, updateDocumentTitle)
</script>

<template>
  <!--
    Заставка как у нативного приложения: только знак, без полосок и подписей. Гайка
    проворачивается, пока поднимается сессия. Текст появляется, лишь когда ждать долго, —
    и говорит конкретно, чего ждём.
  -->
  <div v-if="auth.restoring" class="flex min-h-dvh items-center justify-center bg-canvas px-6">
    <!-- Подпись висит под знаком абсолютно: появляясь, она не сдвигает гайку с места -->
    <div class="relative">
      <LogoMark :size="64" turning class="rounded-[18px] shadow-overlay" />
      <p class="sr-only" role="status">{{ t('app.loading') }}</p>
      <p
        v-if="slow"
        class="absolute top-full left-1/2 mt-6 w-72 -translate-x-1/2 animate-fade-in text-center text-sm leading-relaxed text-ink-3"
        role="status"
      >
        {{ t('app.waking') }}
      </p>
    </div>
  </div>

  <AppShell v-else-if="withShell" />

  <RouterView v-else v-slot="{ Component }">
    <Transition :name="pageTransition" mode="out-in">
      <component :is="Component" />
    </Transition>
  </RouterView>

  <AppToasts />
</template>
