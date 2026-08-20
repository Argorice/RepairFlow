<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import AppToasts from '@/components/ui/AppToasts.vue'
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
</script>

<template>
  <div v-if="auth.restoring" class="flex min-h-screen items-center justify-center bg-slate-50 px-6">
    <div class="flex max-w-sm flex-col items-center gap-3 text-center">
      <div class="size-8 animate-spin rounded-full border-2 border-slate-200 border-t-brand-600" />
      <p class="text-sm text-slate-500">Загружаем рабочее место…</p>
      <p v-if="slow" class="text-xs text-slate-400">
        Демо-сервер просыпается после простоя — это занимает до минуты. Сейчас откроется страница входа.
      </p>
    </div>
  </div>

  <AppShell v-else-if="withShell">
    <RouterView />
  </AppShell>

  <RouterView v-else />

  <AppToasts />
</template>
