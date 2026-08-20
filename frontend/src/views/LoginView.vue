<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError, apiBaseUrl } from '@/api/client'
import { UserRole } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()
const route = useRoute()

const mode = ref<'login' | 'register'>('login')
const email = ref('')
const password = ref('')
const fullName = ref('')
const phone = ref('')

const loading = ref(false)
const demoLoading = ref<UserRole | null>(null)
const error = ref<ApiError | null>(null)

const DEMO_ROLES: { role: UserRole; label: string; hint: string }[] = [
  { role: UserRole.Client, label: 'Войти как клиент', hint: 'Свои заявки и согласование сметы' },
  { role: UserRole.Technician, label: 'Войти как мастер', hint: 'Ремонт, смета, комментарии' },
  { role: UserRole.Manager, label: 'Войти как менеджер', hint: 'Дашборд, назначения, пользователи' },
]

async function go(): Promise<void> {
  const redirect = route.query.redirect
  await router.push(typeof redirect === 'string' ? redirect : { name: 'orders' })
}

async function submit(): Promise<void> {
  loading.value = true
  error.value = null

  try {
    if (mode.value === 'login') {
      await auth.login(email.value, password.value)
    } else {
      await auth.register({
        email: email.value,
        password: password.value,
        fullName: fullName.value,
        phone: phone.value || null,
      })
    }

    await go()
  } catch (exception) {
    error.value = exception instanceof ApiError ? exception : null
    toasts.error(error.value?.message ?? 'Не удалось войти.')
  } finally {
    loading.value = false
  }
}

/**
 * Демо живёт на бесплатном тарифе, где сервис засыпает без трафика и просыпается до минуты.
 * Пингуем его сразу при открытии страницы: пока человек читает текст и выбирает роль,
 * бэкенд успевает подняться. Если не успевает — честно об этом пишем, а не молчим.
 */
const backendWaking = ref(false)

onMounted(() => {
  const hint = window.setTimeout(() => (backendWaking.value = true), 2500)

  void fetch(`${apiBaseUrl}/health`, { mode: 'cors' })
    .catch(() => undefined)
    .finally(() => {
      window.clearTimeout(hint)
      backendWaking.value = false
    })
})

async function loginAsDemo(role: UserRole): Promise<void> {
  demoLoading.value = role

  try {
    await auth.loginAsDemo(role)
    await go()
  } catch (exception) {
    toasts.error(exception instanceof ApiError ? exception.message : 'Демо-вход недоступен.')
  } finally {
    demoLoading.value = null
  }
}
</script>

<template>
  <div class="grid min-h-screen lg:grid-cols-2">
    <!-- Левая половина: что это за система, за пять секунд чтения -->
    <section class="hidden flex-col justify-between bg-brand-600 p-12 text-white lg:flex">
      <p class="font-display text-2xl font-bold">RepairFlow</p>

      <div class="max-w-md">
        <h1 class="font-display text-4xl font-bold leading-tight">
          Сервисный центр, в котором ничего не теряется
        </h1>
        <p class="mt-4 text-brand-100">
          Клиент видит статус ремонта, мастер ведёт смету, менеджер — загрузку мастерской.
          Каждая смена статуса записана: кто, когда и почему.
        </p>

        <ul class="mt-8 space-y-3 text-sm text-brand-100">
          <li class="flex items-start gap-2">
            <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-white" />
            Согласование сметы онлайн — без звонков «ну что там?»
          </li>
          <li class="flex items-start gap-2">
            <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-white" />
            Три роли с разными правами и своей картиной мира
          </li>
          <li class="flex items-start gap-2">
            <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-white" />
            Живые обновления: карточка меняется сама
          </li>
        </ul>
      </div>

      <p class="text-sm text-brand-200">ASP.NET Core 10 · Vue 3 · PostgreSQL</p>
    </section>

    <!-- Правая половина: вход -->
    <section class="flex items-center justify-center px-4 py-12">
      <div class="w-full max-w-sm">
        <p class="font-display text-2xl font-bold text-brand-600 lg:hidden">RepairFlow</p>

        <h2 class="mt-6 font-display text-2xl font-bold lg:mt-0">
          {{ mode === 'login' ? 'Вход' : 'Регистрация' }}
        </h2>
        <p class="mt-1 text-sm text-slate-500">
          {{
            mode === 'login'
              ? 'Посмотреть систему можно без регистрации'
              : 'Регистрация создаёт аккаунт клиента'
          }}
        </p>

        <!-- Главная кнопка демо: заказчик не станет ничего заполнять -->
        <div class="mt-6 space-y-2">
          <AppButton
            v-for="demo in DEMO_ROLES"
            :key="demo.role"
            variant="secondary"
            block
            :loading="demoLoading === demo.role"
            :disabled="demoLoading !== null"
            @click="loginAsDemo(demo.role)"
          >
            <span class="flex w-full items-center justify-between gap-3">
              <span class="font-medium text-slate-900">{{ demo.label }}</span>
              <span class="hidden text-xs text-slate-400 sm:inline">{{ demo.hint }}</span>
            </span>
          </AppButton>
        </div>

        <p
          v-if="backendWaking"
          class="mt-3 rounded-lg bg-slate-100 px-3 py-2 text-xs text-slate-500"
          role="status"
        >
          Демо-сервер просыпается после простоя — первый вход может занять до минуты.
        </p>

        <div class="my-6 flex items-center gap-3 text-xs text-slate-400">
          <span class="h-px flex-1 bg-slate-200" />
          или по паролю
          <span class="h-px flex-1 bg-slate-200" />
        </div>

        <form class="space-y-4" @submit.prevent="submit">
          <AppInput
            v-if="mode === 'register'"
            v-model="fullName"
            label="Имя"
            required
            autocomplete="name"
            :error="error?.fieldError('fullName')"
          />

          <AppInput
            v-model="email"
            label="Почта"
            type="email"
            required
            autocomplete="email"
            placeholder="client@demo.io"
            :error="error?.fieldError('email')"
          />

          <AppInput
            v-model="password"
            label="Пароль"
            type="password"
            required
            :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
            placeholder="demo1234"
            :error="error?.fieldError('password')"
          />

          <AppInput
            v-if="mode === 'register'"
            v-model="phone"
            label="Телефон"
            type="tel"
            autocomplete="tel"
            :error="error?.fieldError('phone')"
          />

          <AppButton type="submit" block :loading="loading">
            {{ mode === 'login' ? 'Войти' : 'Зарегистрироваться' }}
          </AppButton>
        </form>

        <p class="mt-6 text-center text-sm text-slate-500">
          {{ mode === 'login' ? 'Ещё нет аккаунта?' : 'Уже есть аккаунт?' }}
          <button
            class="font-medium text-brand-600 hover:text-brand-700"
            @click="mode = mode === 'login' ? 'register' : 'login'"
          >
            {{ mode === 'login' ? 'Создать' : 'Войти' }}
          </button>
        </p>
      </div>
    </section>
  </div>
</template>
