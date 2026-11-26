<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import BrandMark from '@/components/layout/BrandMark.vue'
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
  <div v-if="auth.restoring" class="flex min-h-dvh items-center justify-center bg-canvas px-6">
    <div class="flex max-w-sm animate-fade-in flex-col items-center gap-5 text-center">
      <BrandMark />
      <div class="h-1 w-32 overflow-hidden rounded-full bg-surface-3" aria-hidden="true">
        <div class="h-full w-1/3 animate-[loading_1.2s_ease-in-out_infinite] rounded-full bg-accent" />
      </div>
      <p class="text-sm text-ink-2" role="status">{{ t('app.loading') }}</p>
      <p v-if="slow" class="text-xs leading-relaxed text-ink-3">{{ t('app.waking') }}</p>
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

<style>
@keyframes loading {
  from {
    transform: translateX(-100%);
  }

  to {
    transform: translateX(300%);
  }
}
</style>
