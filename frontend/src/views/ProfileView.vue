<script setup lang="ts">
import { ref } from 'vue'
import { ApiError } from '@/api/client'
import { usersApi } from '@/api/endpoints'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import { formatDateTime } from '@/lib/format'
import { ROLE_LABELS } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()

const profile = ref({
  fullName: auth.user?.fullName ?? '',
  phone: auth.user?.phone ?? '',
})

const passwords = ref({ current: '', next: '' })

const savingProfile = ref(false)
const savingPassword = ref(false)
const profileError = ref<ApiError | null>(null)
const passwordError = ref<ApiError | null>(null)

async function saveProfile(): Promise<void> {
  savingProfile.value = true
  profileError.value = null

  try {
    const updated = await usersApi.updateProfile({
      fullName: profile.value.fullName,
      phone: profile.value.phone || null,
    })

    auth.patchUser(updated)
    toasts.success('Профиль сохранён')
  } catch (exception) {
    profileError.value = exception instanceof ApiError ? exception : null
    toasts.error(profileError.value?.message ?? 'Не удалось сохранить профиль.')
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
    toasts.success('Пароль изменён, прочие сессии завершены')
  } catch (exception) {
    passwordError.value = exception instanceof ApiError ? exception : null
    toasts.error(passwordError.value?.message ?? 'Не удалось сменить пароль.')
  } finally {
    savingPassword.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-6">
    <header>
      <h1 class="font-display text-2xl font-bold">Профиль</h1>
      <p class="mt-1 text-sm text-slate-500">
        {{ auth.user?.email }} ·
        {{ auth.role ? ROLE_LABELS[auth.role] : '' }} ·
        с {{ formatDateTime(auth.user?.createdAt) }}
      </p>
    </header>

    <form class="card space-y-4 p-6" @submit.prevent="saveProfile">
      <h2 class="font-display text-lg font-semibold">Контакты</h2>

      <AppInput
        v-model="profile.fullName"
        label="Имя"
        required
        :error="profileError?.fieldError('fullName')"
      />
      <AppInput v-model="profile.phone" label="Телефон" type="tel" :error="profileError?.fieldError('phone')" />

      <div class="flex justify-end">
        <AppButton type="submit" :loading="savingProfile">Сохранить</AppButton>
      </div>
    </form>

    <form class="card space-y-4 p-6" @submit.prevent="changePassword">
      <div>
        <h2 class="font-display text-lg font-semibold">Пароль</h2>
        <p class="mt-1 text-sm text-slate-500">
          После смены пароля все остальные сессии завершатся — это правильно, если пароль мог утечь.
        </p>
      </div>

      <AppInput
        v-model="passwords.current"
        label="Текущий пароль"
        type="password"
        autocomplete="current-password"
        required
        :error="passwordError?.fieldError('currentPassword')"
      />
      <AppInput
        v-model="passwords.next"
        label="Новый пароль"
        type="password"
        autocomplete="new-password"
        hint="Не короче 8 символов"
        required
        :error="passwordError?.fieldError('newPassword')"
      />

      <div class="flex justify-end">
        <AppButton type="submit" variant="secondary" :loading="savingPassword">Сменить пароль</AppButton>
      </div>
    </form>
  </div>
</template>
