<script setup lang="ts">
import { LogOut, Monitor, Moon, Sun } from '@lucide/vue'
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/client'
import { usersApi } from '@/api/endpoints'
import PageHeader from '@/components/layout/PageHeader.vue'
import AppAvatar from '@/components/ui/AppAvatar.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppSegmented from '@/components/ui/AppSegmented.vue'
import { locale, setLocale, t, type Locale } from '@/i18n'
import { formatDate } from '@/lib/format'
import { roleLabel } from '@/lib/status'
import { setTheme, themePreference, type ThemePreference } from '@/lib/theme'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()

const profile = ref({
  fullName: auth.user?.fullName ?? '',
  phone: auth.user?.phone ?? '',
})

const passwords = ref({ current: '', next: '' })

const savingProfile = ref(false)
const savingPassword = ref(false)
const profileError = ref<ApiError | null>(null)
const passwordError = ref<ApiError | null>(null)

/** Настройки применяются сразу, без кнопки «Сохранить»: так ведут себя настройки в приложениях. */
const language = computed<Locale>({
  get: () => locale.value,
  set: (value) => setLocale(value),
})

const theme = computed<ThemePreference>({
  get: () => themePreference.value,
  set: (value) => setTheme(value),
})

const languageOptions = [
  { value: 'en' as const, label: 'English' },
  { value: 'ru' as const, label: 'Русский' },
]

const themeOptions = computed(() => [
  { value: 'system' as const, label: t('settings.themes.system'), icon: Monitor },
  { value: 'light' as const, label: t('settings.themes.light'), icon: Sun },
  { value: 'dark' as const, label: t('settings.themes.dark'), icon: Moon },
])

const profileChanged = computed(
  () =>
    profile.value.fullName.trim() !== (auth.user?.fullName ?? '') ||
    (profile.value.phone.trim() || null) !== (auth.user?.phone ?? null),
)

async function saveProfile(): Promise<void> {
  savingProfile.value = true
  profileError.value = null

  try {
    const updated = await usersApi.updateProfile({
      fullName: profile.value.fullName.trim(),
      phone: profile.value.phone.trim() || null,
    })

    auth.patchUser(updated)
    toasts.success(t('profile.saved'))
  } catch (exception) {
    profileError.value = exception instanceof ApiError ? exception : null
    toasts.error(profileError.value?.message ?? t('profile.saveFailed'))
  } finally {
    savingProfile.value = false
  }
}

async function changePassword(): Promise<void> {
  savingPassword.value = true
  passwordError.value = null

  try {
    await usersApi.changePassword(passwords.value.current, passwords.value.next)
    passwords.value = { current: '', next: '' }
    toasts.success(t('profile.passwordChanged'))
  } catch (exception) {
    passwordError.value = exception instanceof ApiError ? exception : null
    toasts.error(passwordError.value?.message ?? t('profile.passwordFailed'))
  } finally {
    savingPassword.value = false
  }
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push({ name: 'login' })
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-5">
    <PageHeader :title="t('nav.profile')" />

    <!-- Карточка аккаунта -->
    <section class="card flex items-center gap-4 p-4 sm:p-6">
      <AppAvatar :name="auth.user?.fullName ?? ''" size="lg" />
      <div class="min-w-0 flex-1">
        <p class="truncate font-display text-lg font-semibold">{{ auth.user?.fullName }}</p>
        <p class="truncate text-sm text-ink-2">{{ auth.user?.email }}</p>
        <p class="mt-1 text-xs text-ink-3">
          {{ auth.role ? roleLabel(auth.role) : '' }} ·
          {{ t('users.since', { date: formatDate(auth.user?.createdAt) }) }}
        </p>
      </div>
    </section>

    <!-- Внешний вид -->
    <section class="card divide-y divide-line">
      <div class="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-6">
        <div>
          <h2 class="section-title">{{ t('settings.language') }}</h2>
          <p class="mt-0.5 text-sm text-ink-2">{{ t('settings.languageHint') }}</p>
        </div>
        <AppSegmented v-model="language" :options="languageOptions" :label="t('settings.language')" block="mobile" />
      </div>

      <div class="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-6">
        <div>
          <h2 class="section-title">{{ t('settings.theme') }}</h2>
          <p class="mt-0.5 text-sm text-ink-2">{{ t('settings.themeHint') }}</p>
        </div>
        <AppSegmented v-model="theme" :options="themeOptions" :label="t('settings.theme')" block="mobile" />
      </div>
    </section>

    <form class="card space-y-4 p-4 sm:p-6" @submit.prevent="saveProfile">
      <h2 class="section-title">{{ t('profile.contacts') }}</h2>

      <AppInput
        v-model="profile.fullName"
        :label="t('fields.fullName')"
        autocomplete="name"
        required
        :error="profileError?.fieldError('fullName')"
      />
      <AppInput
        v-model="profile.phone"
        :label="t('fields.phone')"
        type="tel"
        inputmode="tel"
        autocomplete="tel"
        :error="profileError?.fieldError('phone')"
      />

      <div class="flex justify-end">
        <AppButton type="submit" class="w-full sm:w-auto" :loading="savingProfile" :disabled="!profileChanged">
          {{ t('common.save') }}
        </AppButton>
      </div>
    </form>

    <form class="card space-y-4 p-4 sm:p-6" @submit.prevent="changePassword">
      <div>
        <h2 class="section-title">{{ t('profile.password') }}</h2>
        <p class="mt-1 text-sm text-ink-2">{{ t('profile.passwordHint') }}</p>
      </div>

      <AppInput
        v-model="passwords.current"
        :label="t('profile.currentPassword')"
        type="password"
        autocomplete="current-password"
        required
        :error="passwordError?.fieldError('currentPassword')"
      />
      <AppInput
        v-model="passwords.next"
        :label="t('profile.newPassword')"
        type="password"
        autocomplete="new-password"
        :hint="t('fields.passwordHint')"
        required
        :error="passwordError?.fieldError('newPassword')"
      />

      <div class="flex justify-end">
        <AppButton
          type="submit"
          variant="secondary"
          class="w-full sm:w-auto"
          :loading="savingPassword"
          :disabled="!passwords.current || !passwords.next"
        >
          {{ t('profile.changePassword') }}
        </AppButton>
      </div>
    </form>

    <!-- На телефоне бокового меню нет — выход живёт здесь -->
    <div class="lg:hidden">
      <AppButton variant="danger" size="lg" block :icon="LogOut" @click="logout">
        {{ t('nav.signOut') }}
      </AppButton>
    </div>
  </div>
</template>
