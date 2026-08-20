<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { initials } from '@/lib/format'
import { ROLE_LABELS } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const menuOpen = ref(false)

interface NavItem {
  name: string
  label: string
  visible: boolean
}

const items = computed<NavItem[]>(() => [
  { name: 'dashboard', label: 'Дашборд', visible: auth.isManager },
  { name: 'orders', label: 'Заявки', visible: true },
  { name: 'users', label: 'Пользователи', visible: auth.isManager },
  { name: 'profile', label: 'Профиль', visible: true },
])

const visibleItems = computed(() => items.value.filter((item) => item.visible))

function isActive(name: string): boolean {
  return route.name === name || (name === 'orders' && String(route.name).startsWith('order'))
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen bg-slate-50">
    <!-- Мобильная шапка -->
    <header class="sticky top-0 z-30 flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
      <RouterLink :to="{ name: 'orders' }" class="font-display text-lg font-bold text-brand-600">
        RepairFlow
      </RouterLink>

      <button
        class="rounded-lg p-2 text-slate-600 hover:bg-slate-100"
        :aria-expanded="menuOpen"
        aria-label="Меню"
        @click="menuOpen = !menuOpen"
      >
        <svg class="size-5" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
      </button>
    </header>

    <nav v-if="menuOpen" class="border-b border-slate-200 bg-white px-4 pb-4 lg:hidden">
      <RouterLink
        v-for="item in visibleItems"
        :key="item.name"
        :to="{ name: item.name }"
        class="block rounded-lg px-3 py-2 text-sm font-medium"
        :class="isActive(item.name) ? 'bg-brand-50 text-brand-700' : 'text-slate-600'"
        @click="menuOpen = false"
      >
        {{ item.label }}
      </RouterLink>
      <button class="mt-2 w-full rounded-lg px-3 py-2 text-left text-sm text-slate-500" @click="logout">
        Выйти
      </button>
    </nav>

    <div class="mx-auto flex w-full max-w-[1400px]">
      <!-- Боковое меню -->
      <aside class="sticky top-0 hidden h-screen w-60 shrink-0 flex-col border-r border-slate-200 bg-white px-4 py-6 lg:flex">
        <RouterLink :to="{ name: 'orders' }" class="mb-8 px-3 font-display text-xl font-bold text-brand-600">
          RepairFlow
        </RouterLink>

        <nav class="flex flex-1 flex-col gap-1">
          <RouterLink
            v-for="item in visibleItems"
            :key="item.name"
            :to="{ name: item.name }"
            class="rounded-lg px-3 py-2 text-sm font-medium transition-colors"
            :class="
              isActive(item.name)
                ? 'bg-brand-50 text-brand-700'
                : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
            "
          >
            {{ item.label }}
          </RouterLink>
        </nav>

        <div class="mt-6 border-t border-slate-200 pt-4">
          <div class="flex items-center gap-3 px-3">
            <span class="flex size-9 items-center justify-center rounded-full bg-brand-50 text-sm font-semibold text-brand-700">
              {{ auth.user ? initials(auth.user.fullName) : '?' }}
            </span>
            <div class="min-w-0">
              <p class="truncate text-sm font-medium text-slate-900">{{ auth.user?.fullName }}</p>
              <p class="truncate text-xs text-slate-500">
                {{ auth.role ? ROLE_LABELS[auth.role] : '' }}
              </p>
            </div>
          </div>

          <button
            class="mt-3 w-full rounded-lg px-3 py-2 text-left text-sm text-slate-500 hover:bg-slate-50"
            @click="logout"
          >
            Выйти
          </button>
        </div>
      </aside>

      <main class="min-w-0 flex-1 px-4 py-6 lg:px-8">
        <slot />
      </main>
    </div>
  </div>
</template>
