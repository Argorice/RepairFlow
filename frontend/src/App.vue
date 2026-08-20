<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import AppToasts from '@/components/ui/AppToasts.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()

const withShell = computed(() => auth.isAuthenticated && route.meta.public !== true)
</script>

<template>
  <!-- Пока поднимается сессия, показываем не спиннер по центру пустоты, а спокойную заглушку. -->
  <div v-if="auth.restoring" class="flex min-h-screen items-center justify-center bg-slate-50">
    <div class="flex flex-col items-center gap-3">
      <div class="size-8 animate-spin rounded-full border-2 border-slate-200 border-t-brand-600" />
      <p class="text-sm text-slate-500">Загружаем рабочее место…</p>
    </div>
  </div>

  <AppShell v-else-if="withShell">
    <RouterView />
  </AppShell>

  <RouterView v-else />

  <AppToasts />
</template>
