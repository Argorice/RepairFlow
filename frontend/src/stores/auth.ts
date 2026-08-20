import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { refreshSession, setAccessToken } from '@/api/client'
import { authApi } from '@/api/endpoints'
import { UserRole, type UserDto } from '@/api/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<UserDto | null>(null)

  /** Пока идёт восстановление сессии, роутер не должен принимать решений о редиректах. */
  const restoring = ref(true)

  const isAuthenticated = computed(() => user.value !== null)
  const role = computed(() => user.value?.role ?? null)
  const isManager = computed(() => role.value === UserRole.Manager)
  const isTechnician = computed(() => role.value === UserRole.Technician)
  const isClient = computed(() => role.value === UserRole.Client)
  const isStaff = computed(() => isManager.value || isTechnician.value)

  /**
   * Access-токен живёт в памяти, поэтому после F5 его нет. Зато есть httpOnly-кука
   * с refresh-токеном — по ней молча поднимаем сессию до первой отрисовки.
   */
  async function restore(): Promise<void> {
    restoring.value = true

    try {
      const token = await refreshSession()
      user.value = token ? await authApi.me() : null
    } catch {
      user.value = null
    } finally {
      restoring.value = false
    }
  }

  async function login(email: string, password: string): Promise<void> {
    const response = await authApi.login(email, password)
    setAccessToken(response.accessToken)
    user.value = response.user
  }

  async function loginAsDemo(demoRole: UserRole): Promise<void> {
    const response = await authApi.demo(demoRole)
    setAccessToken(response.accessToken)
    user.value = response.user
  }

  async function register(payload: {
    email: string
    password: string
    fullName: string
    phone?: string | null
  }): Promise<void> {
    const response = await authApi.register(payload)
    setAccessToken(response.accessToken)
    user.value = response.user
  }

  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } finally {
      clear()
    }
  }

  function clear(): void {
    setAccessToken(null)
    user.value = null
  }

  function patchUser(updated: UserDto): void {
    user.value = updated
  }

  return {
    user,
    restoring,
    isAuthenticated,
    role,
    isManager,
    isTechnician,
    isClient,
    isStaff,
    restore,
    login,
    loginAsDemo,
    register,
    logout,
    clear,
    patchUser,
  }
})
