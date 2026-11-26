import { ref } from 'vue'
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { UserRole } from '@/api/types'
import { t, type MessageKey } from '@/i18n'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    /** Публичный маршрут: доступен без авторизации. */
    public?: boolean
    /** Роли, которым маршрут разрешён. Пусто — разрешён всем авторизованным. */
    roles?: UserRole[]
    titleKey?: MessageKey
    /**
     * Глубина экрана. 1 — раздел из нижнего меню, 2 — экран внутри раздела: у него кнопка
     * «назад» вместо меню, и переход к нему анимируется «вглубь».
     */
    depth?: 1 | 2
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true, titleKey: 'nav.signIn' },
  },
  {
    path: '/',
    name: 'home',
    redirect: () => ({ name: 'orders' }),
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: () => import('@/views/DashboardView.vue'),
    meta: { roles: [UserRole.Manager], titleKey: 'nav.dashboard', depth: 1 },
  },
  {
    path: '/orders',
    name: 'orders',
    component: () => import('@/views/OrdersView.vue'),
    meta: { titleKey: 'nav.orders', depth: 1 },
  },
  {
    path: '/orders/new',
    name: 'order-create',
    component: () => import('@/views/OrderCreateView.vue'),
    meta: { roles: [UserRole.Client], titleKey: 'create.title', depth: 2 },
  },
  {
    path: '/orders/:id',
    name: 'order-details',
    component: () => import('@/views/OrderDetailsView.vue'),
    meta: { titleKey: 'details.title', depth: 2 },
  },
  {
    path: '/users',
    name: 'users',
    component: () => import('@/views/UsersView.vue'),
    meta: { roles: [UserRole.Manager], titleKey: 'nav.users', depth: 1 },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('@/views/ProfileView.vue'),
    meta: { titleKey: 'nav.profile', depth: 1 },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { public: true, titleKey: 'notFound.title' },
  },
]

/** Имя CSS-перехода для следующей смены экрана — его читает оболочка приложения. */
export const pageTransition = ref<'page-forward' | 'page-back' | 'page-fade'>('page-fade')

const wide = window.matchMedia('(min-width: 1024px)')

/** Сколько длится переход между экранами: позицию прокрутки восстанавливаем после него. */
const TRANSITION_MS = 240

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior(to, from, savedPosition) {
    // Смена фильтров меняет только query — прокрутку при этом не трогаем.
    if (to.path === from.path) {
      return false
    }

    // «Назад» к списку возвращает туда же, где человек остановился, — как в любом приложении.
    if (savedPosition) {
      return new Promise((resolve) => window.setTimeout(() => resolve(savedPosition), TRANSITION_MS))
    }

    return { top: 0 }
  },
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Первый переход случается раньше, чем восстановится сессия, — дожидаемся её.
  if (auth.restoring) {
    await auth.restore()
  }

  if (to.meta.public) {
    // Авторизованному на странице входа делать нечего.
    return to.name === 'login' && auth.isAuthenticated ? { name: 'orders' } : true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  const allowed = to.meta.roles
  if (allowed && allowed.length > 0 && (!auth.role || !allowed.includes(auth.role))) {
    return { name: 'orders' }
  }

  return true
})

router.afterEach((to, from) => {
  const toDepth = to.meta.depth ?? 1
  const fromDepth = from.meta.depth ?? 1

  // На широком экране экраны не «въезжают» — там нет стека навигации, только растворение.
  pageTransition.value =
    wide.matches || !from.name || toDepth === fromDepth
      ? 'page-fade'
      : toDepth > fromDepth
        ? 'page-forward'
        : 'page-back'

  updateDocumentTitle()
})

/** Заголовок вкладки браузера. Вызывается и при смене языка. */
export function updateDocumentTitle(): void {
  const key = router.currentRoute.value.meta.titleKey
  document.title = key ? `${t(key)} · RepairFlow` : 'RepairFlow'
}
