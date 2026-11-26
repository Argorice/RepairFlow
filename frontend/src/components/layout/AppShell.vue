<script setup lang="ts">
import {
  ChevronLeft,
  ClipboardList,
  LayoutDashboard,
  LogOut,
  Plus,
  UserRound,
  Users,
} from '@lucide/vue'
import { computed, type Component } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { t, type MessageKey } from '@/i18n'
import { appBar } from '@/lib/appBar'
import { roleLabel } from '@/lib/status'
import { pageTransition } from '@/router'
import { useAuthStore } from '@/stores/auth'
import AppAvatar from '@/components/ui/AppAvatar.vue'
import BrandMark from './BrandMark.vue'
import PreferenceToggles from './PreferenceToggles.vue'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

interface NavItem {
  name: string
  labelKey: MessageKey
  icon: Component
  /** Главное действие раздела — в нижнем меню рисуется выделенной кнопкой. */
  primary?: boolean
}

/** Набор разделов зависит от роли: у каждой своя картина мира, лишних пунктов нет. */
const items = computed<NavItem[]>(() => {
  if (auth.isManager) {
    return [
      { name: 'dashboard', labelKey: 'nav.dashboard', icon: LayoutDashboard },
      { name: 'orders', labelKey: 'nav.orders', icon: ClipboardList },
      { name: 'users', labelKey: 'nav.users', icon: Users },
      { name: 'profile', labelKey: 'nav.profile', icon: UserRound },
    ]
  }

  if (auth.isClient) {
    return [
      { name: 'orders', labelKey: 'nav.myOrders', icon: ClipboardList },
      { name: 'order-create', labelKey: 'nav.newOrder', icon: Plus, primary: true },
      { name: 'profile', labelKey: 'nav.profile', icon: UserRound },
    ]
  }

  return [
    { name: 'orders', labelKey: 'nav.orders', icon: ClipboardList },
    { name: 'profile', labelKey: 'nav.profile', icon: UserRound },
  ]
})

const sidebarItems = computed(() => items.value.filter((item) => !item.primary))

function isActive(name: string): boolean {
  return route.name === name || (name === 'orders' && route.name === 'order-details')
}

const isNested = computed(() => route.meta.depth === 2)

const barTitle = computed(() => {
  const fallback = route.meta.titleKey ? t(route.meta.titleKey) : 'RepairFlow'
  return appBar.title || fallback
})

/** Заголовок в панели виден, когда крупного заголовка на экране нет или он уехал вверх. */
const showBarTitle = computed(() => !appBar.hasLargeTitle || !appBar.largeTitleVisible)

function goBack(): void {
  // Пришли по прямой ссылке — «назад» в истории браузера увёл бы с сайта. Ведём в список.
  if (window.history.state?.back) {
    router.back()
  } else {
    void router.push({ name: 'orders' })
  }
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}

/**
 * Список заявок держится в памяти, пока открыта карточка: «назад» возвращает к тому же месту
 * мгновенно, а свежие данные список подтягивает сам, когда снова становится видимым.
 */
const KEEP_ALIVE = ['OrdersView']

function viewKey(name: unknown, id: unknown): string {
  return `${String(name)}:${id ? String(id) : ''}`
}
</script>

<template>
  <div class="min-h-dvh bg-canvas lg:flex">
    <!-- Боковое меню: широкий экран -->
    <aside
      class="sticky top-0 hidden h-dvh w-64 shrink-0 flex-col border-r border-line bg-surface px-3 py-5 lg:flex"
    >
      <RouterLink :to="{ name: 'orders' }" class="mb-7 px-3" :aria-label="t('nav.home')">
        <BrandMark />
      </RouterLink>

      <RouterLink
        v-if="auth.isClient"
        :to="{ name: 'order-create' }"
        class="pressable mx-1 mb-5 flex h-10 items-center justify-center gap-2 rounded-xl bg-accent text-sm font-medium text-on-accent hover:bg-accent-hover"
      >
        <Plus class="size-[18px]" aria-hidden="true" />
        {{ t('nav.newOrder') }}
      </RouterLink>

      <nav class="flex flex-1 flex-col gap-0.5" :aria-label="t('nav.main')">
        <RouterLink
          v-for="item in sidebarItems"
          :key="item.name"
          :to="{ name: item.name }"
          class="flex h-10 items-center gap-3 rounded-xl px-3 text-sm font-medium transition-colors"
          :class="
            isActive(item.name)
              ? 'bg-accent-soft text-accent-ink'
              : 'text-ink-2 hover:bg-surface-2 hover:text-ink'
          "
          :aria-current="isActive(item.name) ? 'page' : undefined"
        >
          <component :is="item.icon" class="size-[18px]" aria-hidden="true" />
          {{ t(item.labelKey) }}
        </RouterLink>
      </nav>

      <div class="mt-4 space-y-3 border-t border-line pt-4">
        <div class="flex items-center justify-between px-1">
          <span class="px-2 text-xs text-ink-3">{{ t('settings.title') }}</span>
          <PreferenceToggles />
        </div>

        <div class="flex items-center gap-3 rounded-xl px-2 py-1">
          <AppAvatar :name="auth.user?.fullName ?? ''" size="sm" />
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-medium text-ink">{{ auth.user?.fullName }}</p>
            <p class="truncate text-xs text-ink-3">{{ auth.role ? roleLabel(auth.role) : '' }}</p>
          </div>
          <button
            type="button"
            class="pressable rounded-lg p-2 text-ink-3 hover:bg-surface-2 hover:text-ink"
            :aria-label="t('nav.signOut')"
            :title="t('nav.signOut')"
            @click="logout"
          >
            <LogOut class="size-[18px]" aria-hidden="true" />
          </button>
        </div>
      </div>
    </aside>

    <div class="min-w-0 flex-1">
      <!-- Верхняя панель: телефон и планшет -->
      <header
        class="sticky top-0 z-30 border-b border-line bg-[var(--rf-glass)] pt-safe backdrop-blur-xl backdrop-saturate-150 lg:hidden"
      >
        <div class="relative flex h-14 items-center gap-1 px-2">
          <button
            v-if="isNested"
            type="button"
            class="pressable flex h-10 items-center gap-0.5 rounded-xl pr-3 pl-1 text-accent-ink hover:bg-surface-2"
            @click="goBack"
          >
            <ChevronLeft class="size-6" aria-hidden="true" />
            <span class="text-[15px] font-medium">{{ t('common.back') }}</span>
          </button>

          <RouterLink v-else :to="{ name: 'orders' }" class="px-2" :aria-label="t('nav.home')">
            <BrandMark compact />
          </RouterLink>

          <p
            class="pointer-events-none absolute inset-x-24 truncate text-center font-display text-[17px] font-semibold text-ink transition-opacity duration-200"
            :class="showBarTitle ? 'opacity-100' : 'opacity-0'"
            :aria-hidden="!showBarTitle"
          >
            {{ barTitle }}
          </p>

          <!-- Сюда экраны выносят свои кнопки через Teleport -->
          <div id="appbar-actions" class="ml-auto flex items-center gap-1" />
        </div>
      </header>

      <main
        class="mx-auto w-full max-w-[1280px] px-4 pt-4 sm:px-6 lg:px-10 lg:pt-8 lg:pb-12"
        :class="isNested ? 'pb-8' : 'pb-[calc(6rem+env(safe-area-inset-bottom))]'"
      >
        <RouterView v-slot="{ Component, route: current }">
          <Transition :name="pageTransition" mode="out-in">
            <KeepAlive :include="KEEP_ALIVE">
              <component :is="Component" :key="viewKey(current.name, current.params.id)" />
            </KeepAlive>
          </Transition>
        </RouterView>
      </main>
    </div>

    <!-- Нижнее меню: телефон и планшет. Внутри раздела прячется — там кнопка «назад». -->
    <Transition
      enter-active-class="transition duration-200 ease-[var(--ease-app)]"
      enter-from-class="translate-y-full"
      leave-active-class="transition duration-150"
      leave-to-class="translate-y-full"
    >
      <nav
        v-if="!isNested"
        class="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-[var(--rf-glass)] pb-safe backdrop-blur-xl backdrop-saturate-150 lg:hidden"
        :aria-label="t('nav.main')"
      >
        <ul class="mx-auto flex h-16 max-w-md items-stretch justify-around px-2">
          <li v-for="item in items" :key="item.name" class="flex flex-1">
            <RouterLink
              :to="{ name: item.name }"
              class="pressable flex flex-1 flex-col items-center justify-center gap-1"
              :aria-current="isActive(item.name) ? 'page' : undefined"
            >
              <span
                v-if="item.primary"
                class="flex h-9 w-14 items-center justify-center rounded-2xl bg-accent text-on-accent"
              >
                <component :is="item.icon" class="size-5" stroke-width="2.25" aria-hidden="true" />
              </span>
              <span
                v-else
                class="flex h-8 w-14 items-center justify-center rounded-full transition-colors"
                :class="isActive(item.name) ? 'bg-accent-soft text-accent-ink' : 'text-ink-3'"
              >
                <component :is="item.icon" class="size-[22px]" aria-hidden="true" />
              </span>
              <span
                class="text-[11px] leading-none font-medium"
                :class="isActive(item.name) || item.primary ? 'text-ink' : 'text-ink-3'"
              >
                {{ t(item.labelKey) }}
              </span>
            </RouterLink>
          </li>
        </ul>
      </nav>
    </Transition>
  </div>
</template>
