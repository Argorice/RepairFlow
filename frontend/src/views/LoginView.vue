<script setup lang="ts">
import { ArrowRight, Hammer, LayoutDashboard, Lock, Mail, Smartphone } from '@lucide/vue'
import { onMounted, ref, type Component } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError, apiBaseUrl } from '@/api/client'
import { UserRole } from '@/api/types'
import BrandMark from '@/components/layout/BrandMark.vue'
import PreferenceToggles from '@/components/layout/PreferenceToggles.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppSegmented from '@/components/ui/AppSegmented.vue'
import { t, type MessageKey } from '@/i18n'
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

const DEMO_ROLES: { role: UserRole; icon: Component; label: MessageKey; hint: MessageKey }[] = [
  { role: UserRole.Client, icon: Smartphone, label: 'login.demoClient', hint: 'login.demoClientHint' },
  { role: UserRole.Technician, icon: Hammer, label: 'login.demoTechnician', hint: 'login.demoTechnicianHint' },
  { role: UserRole.Manager, icon: LayoutDashboard, label: 'login.demoManager', hint: 'login.demoManagerHint' },
]

const FEATURES: MessageKey[] = ['login.feature1', 'login.feature2', 'login.feature3']

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
    toasts.error(error.value?.message ?? t('login.failed'))
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
    toasts.error(exception instanceof ApiError ? exception.message : t('login.demoFailed'))
  } finally {
    demoLoading.value = null
  }
}
</script>

<template>
  <div class="min-h-dvh bg-canvas lg:grid lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
    <!-- Левая половина: что это за система, за пять секунд чтения -->
    <section
      class="relative overflow-hidden bg-brand-600 px-5 pt-[max(1.25rem,env(safe-area-inset-top))] pb-16 text-white lg:flex lg:flex-col lg:justify-between lg:p-12"
    >
      <!-- Фирменный узор: крупные полупрозрачные круги, плоско, без градиентов -->
      <div class="pointer-events-none absolute -top-24 -right-24 size-72 rounded-full bg-white/[0.06]" aria-hidden="true" />
      <div class="pointer-events-none absolute -bottom-32 -left-16 size-80 rounded-full bg-white/[0.05]" aria-hidden="true" />

      <div class="relative flex items-center justify-between">
        <BrandMark inverted />
        <PreferenceToggles inverted class="lg:hidden" />
      </div>

      <div class="relative mt-8 max-w-md lg:mt-0">
        <h1 class="font-display text-[28px] leading-tight font-bold !text-white lg:text-[40px]">
          {{ t('login.headline') }}
        </h1>
        <p class="mt-3 text-[15px] leading-relaxed text-brand-100 lg:mt-4 lg:text-base">
          {{ t('login.lead') }}
        </p>

        <ul class="mt-8 hidden space-y-3 text-sm text-brand-100 lg:block">
          <li v-for="feature in FEATURES" :key="feature" class="flex items-start gap-2.5">
            <span class="mt-1.5 size-1.5 shrink-0 rounded-full bg-white" aria-hidden="true" />
            {{ t(feature) }}
          </li>
        </ul>
      </div>

      <p class="relative hidden text-sm text-brand-200 lg:block">ASP.NET Core 10 · Vue 3 · PostgreSQL · SignalR</p>
    </section>

    <!-- Правая половина: вход. На телефоне — карточка, которая наезжает на синюю шапку -->
    <section class="relative -mt-8 flex justify-center px-4 pb-[max(2rem,env(safe-area-inset-bottom))] lg:mt-0 lg:items-center lg:px-10 lg:py-12">
      <div class="w-full max-w-md rounded-3xl bg-surface p-5 shadow-overlay sm:p-8 lg:rounded-none lg:bg-transparent lg:p-0 lg:shadow-none">
        <div class="mb-8 hidden justify-end lg:flex">
          <PreferenceToggles />
        </div>

        <h2 class="font-display text-xl font-bold">{{ t('login.demoTitle') }}</h2>
        <p class="mt-1 text-sm text-ink-2">{{ t('login.demoSubtitle') }}</p>

        <!-- Главная кнопка демо: заказчик не станет ничего заполнять -->
        <div class="mt-5 grid gap-2">
          <button
            v-for="demo in DEMO_ROLES"
            :key="demo.role"
            type="button"
            class="pressable group flex items-center gap-3 rounded-2xl bg-surface px-3.5 py-3 text-left ring-1 ring-line transition-colors ring-inset hover:bg-surface-2 hover:ring-accent-line disabled:opacity-60"
            :disabled="demoLoading !== null"
            :aria-busy="demoLoading === demo.role || undefined"
            @click="loginAsDemo(demo.role)"
          >
            <span class="flex size-10 shrink-0 items-center justify-center rounded-xl bg-accent-soft text-accent-ink">
              <component :is="demo.icon" class="size-5" aria-hidden="true" />
            </span>
            <span class="min-w-0 flex-1">
              <span class="block text-sm font-semibold text-ink">{{ t(demo.label) }}</span>
              <span class="block truncate text-xs text-ink-3">{{ t(demo.hint) }}</span>
            </span>
            <span
              v-if="demoLoading === demo.role"
              class="size-5 animate-spin rounded-full border-2 border-accent-line border-t-accent"
              aria-hidden="true"
            />
            <ArrowRight
              v-else
              class="size-[18px] text-ink-3 transition-transform group-hover:translate-x-0.5 group-hover:text-accent-ink"
              aria-hidden="true"
            />
          </button>
        </div>

        <p
          v-if="backendWaking"
          class="mt-3 animate-fade-in rounded-xl bg-surface-2 px-3 py-2.5 text-xs leading-relaxed text-ink-2"
          role="status"
        >
          {{ t('login.waking') }}
        </p>

        <div class="my-7 flex items-center gap-3 text-xs text-ink-3">
          <span class="h-px flex-1 bg-line" />
          {{ t('login.orWithPassword') }}
          <span class="h-px flex-1 bg-line" />
        </div>

        <AppSegmented
          v-model="mode"
          :label="t('login.mode')"
          :options="[
            { value: 'login', label: t('login.signIn') },
            { value: 'register', label: t('login.register') },
          ]"
          block
        />

        <form class="mt-5 space-y-4" @submit.prevent="submit">
          <AppInput
            v-if="mode === 'register'"
            v-model="fullName"
            :label="t('fields.fullName')"
            required
            autocomplete="name"
            :error="error?.fieldError('fullName')"
          />

          <AppInput
            v-model="email"
            :label="t('fields.email')"
            type="email"
            :icon="Mail"
            required
            autocomplete="email"
            inputmode="email"
            placeholder="client@demo.io"
            :error="error?.fieldError('email')"
          />

          <AppInput
            v-model="password"
            :label="t('fields.password')"
            type="password"
            :icon="Lock"
            required
            :autocomplete="mode === 'login' ? 'current-password' : 'new-password'"
            :placeholder="mode === 'login' ? 'demo1234' : undefined"
            :hint="mode === 'register' ? t('fields.passwordHint') : undefined"
            :error="error?.fieldError('password')"
          />

          <AppInput
            v-if="mode === 'register'"
            v-model="phone"
            :label="t('fields.phone')"
            type="tel"
            inputmode="tel"
            autocomplete="tel"
            :error="error?.fieldError('phone')"
          />

          <AppButton type="submit" size="lg" block :loading="loading">
            {{ mode === 'login' ? t('login.signIn') : t('login.createAccount') }}
          </AppButton>

          <p v-if="mode === 'register'" class="text-center text-xs text-ink-3">{{ t('login.registerNote') }}</p>
        </form>
      </div>
    </section>
  </div>
</template>
