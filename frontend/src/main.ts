import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { setSessionExpiredHandler } from '@/api/client'
import { router } from '@/router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'
import App from './App.vue'
import './style.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// Если refresh не помог — сессия действительно закончилась: чистим состояние и уводим на вход.
setSessionExpiredHandler(() => {
  const auth = useAuthStore(pinia)
  const toasts = useToastStore(pinia)

  if (auth.isAuthenticated) {
    auth.clear()
    toasts.info('Сессия истекла, войдите заново.')
  }

  if (router.currentRoute.value.name !== 'login') {
    void router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
  }
})

app.mount('#app')
