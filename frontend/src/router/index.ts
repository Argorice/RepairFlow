import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { UserRole } from '@/api/types'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    /** Публичный маршрут: доступен без авторизации. */
    public?: boolean
    /** Роли, которым маршрут разрешён. Пусто — разрешён всем авторизованным. */
    roles?: UserRole[]
    title?: string
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { public: true, title: 'Вход' },
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
    meta: { roles: [UserRole.Manager], title: 'Дашборд' },
  },
  {
    path: '/orders',
    name: 'orders',
    component: () => import('@/views/OrdersView.vue'),
    meta: { title: 'Заявки' },
  },
  {
    path: '/orders/new',
    name: 'order-create',
    component: () => import('@/views/OrderCreateView.vue'),
    meta: { roles: [UserRole.Client], title: 'Новая заявка' },
  },
  {
    path: '/orders/:id',
    name: 'order-details',
    component: () => import('@/views/OrderDetailsView.vue'),
    meta: { title: 'Заявка' },
  },
  {
    path: '/users',
    name: 'users',
    component: () => import('@/views/UsersView.vue'),
    meta: { roles: [UserRole.Manager], title: 'Пользователи' },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('@/views/ProfileView.vue'),
    meta: { title: 'Профиль' },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { public: true, title: 'Страница не найдена' },
  },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
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

router.afterEach((to) => {
  const title = to.meta.title
  document.title = title ? `${title} · RepairFlow` : 'RepairFlow'
})
