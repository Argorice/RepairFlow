<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { usersApi } from '@/api/endpoints'
import { UserRole, type UserDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatDateTime, initials } from '@/lib/format'
import { ROLE_LABELS } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()

const users = ref<UserDto[]>([])
const loading = ref(true)
const search = ref('')
const roleFilter = ref('')

const creating = ref(false)
const saving = ref(false)
const createError = ref<ApiError | null>(null)
const draft = ref({
  email: '',
  password: '',
  fullName: '',
  phone: '',
  role: UserRole.Technician as UserRole,
})

const roleOptions = Object.entries(ROLE_LABELS).map(([value, label]) => ({ value, label }))
const staffRoleOptions = roleOptions.filter((option) => option.value !== UserRole.Client)

async function load(): Promise<void> {
  loading.value = true

  try {
    users.value = await usersApi.list({
      role: (roleFilter.value as UserRole) || undefined,
      search: search.value || undefined,
    })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить пользователей.')
  } finally {
    loading.value = false
  }
}

async function changeRole(user: UserDto, role: UserRole): Promise<void> {
  try {
    const updated = await usersApi.update(user.id, { role })
    users.value = users.value.map((item) => (item.id === updated.id ? updated : item))
    toasts.success(`${updated.fullName}: роль изменена`)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось изменить роль.')
    await load()
  }
}

async function toggleActive(user: UserDto): Promise<void> {
  try {
    const updated = await usersApi.update(user.id, { isActive: !user.isActive })
    users.value = users.value.map((item) => (item.id === updated.id ? updated : item))
    toasts.success(updated.isActive ? 'Учётная запись включена' : 'Учётная запись отключена')
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось изменить статус.')
  }
}

async function create(): Promise<void> {
  saving.value = true
  createError.value = null

  try {
    await usersApi.create({
      email: draft.value.email,
      password: draft.value.password,
      fullName: draft.value.fullName,
      phone: draft.value.phone || null,
      role: draft.value.role,
    })

    creating.value = false
    draft.value = { email: '', password: '', fullName: '', phone: '', role: UserRole.Technician }
    toasts.success('Сотрудник добавлен')
    await load()
  } catch (exception) {
    createError.value = exception instanceof ApiError ? exception : null
    toasts.error(createError.value?.message ?? 'Не удалось создать пользователя.')
  } finally {
    saving.value = false
  }
}

let debounce: number | undefined

function onFilterChange(): void {
  window.clearTimeout(debounce)
  debounce = window.setTimeout(load, 250)
}

onMounted(load)
</script>

<template>
  <div class="space-y-6">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="font-display text-2xl font-bold">Пользователи</h1>
        <p class="mt-1 text-sm text-slate-500">Роли, доступ и контакты сотрудников и клиентов</p>
      </div>

      <AppButton @click="creating = true">Добавить сотрудника</AppButton>
    </header>

    <div class="card grid gap-3 p-4 sm:grid-cols-2">
      <AppInput v-model="search" label="Поиск" placeholder="Имя или почта" @update:model-value="onFilterChange" />
      <AppSelect
        v-model="roleFilter"
        label="Роль"
        placeholder="Все роли"
        :options="roleOptions"
        @update:model-value="onFilterChange"
      />
    </div>

    <div class="card overflow-hidden">
      <SkeletonBlock v-if="loading" :lines="6" class="p-6" />

      <EmptyState
        v-else-if="users.length === 0"
        title="Никого не найдено"
        description="Измените фильтры или добавьте сотрудника."
      />

      <ul v-else class="divide-y divide-slate-100">
        <li
          v-for="user in users"
          :key="user.id"
          class="flex flex-wrap items-center gap-4 px-4 py-4"
          :class="user.isActive ? '' : 'opacity-60'"
        >
          <span class="flex size-10 shrink-0 items-center justify-center rounded-full bg-slate-100 text-sm font-semibold text-slate-600">
            {{ initials(user.fullName) }}
          </span>

          <div class="min-w-0 flex-1">
            <p class="truncate font-medium text-slate-900">
              {{ user.fullName }}
              <span v-if="user.id === auth.user?.id" class="ml-1 text-xs text-slate-400">— это вы</span>
            </p>
            <p class="truncate text-sm text-slate-500">{{ user.email }}</p>
            <p class="text-xs text-slate-400">
              {{ user.phone ?? 'Телефон не указан' }} · с {{ formatDateTime(user.createdAt) }}
            </p>
          </div>

          <div class="w-40">
            <AppSelect
              :model-value="user.role"
              :options="roleOptions"
              :disabled="user.id === auth.user?.id"
              @update:model-value="(value) => changeRole(user, value as UserRole)"
            />
          </div>

          <AppButton
            variant="ghost"
            size="sm"
            :disabled="user.id === auth.user?.id"
            @click="toggleActive(user)"
          >
            {{ user.isActive ? 'Отключить' : 'Включить' }}
          </AppButton>
        </li>
      </ul>
    </div>

    <AppModal
      v-if="creating"
      title="Новый сотрудник"
      description="Пароль сотрудник сможет сменить в своём профиле."
      @close="creating = false"
    >
      <div class="space-y-4">
        <AppInput v-model="draft.fullName" label="Имя" required :error="createError?.fieldError('fullName')" />
        <AppInput
          v-model="draft.email"
          label="Почта"
          type="email"
          required
          :error="createError?.fieldError('email')"
        />
        <AppInput
          v-model="draft.password"
          label="Пароль"
          type="password"
          required
          hint="Не короче 8 символов"
          :error="createError?.fieldError('password')"
        />
        <AppInput v-model="draft.phone" label="Телефон" type="tel" />
        <AppSelect v-model="draft.role" label="Роль" :options="staffRoleOptions" />
      </div>

      <template #actions>
        <AppButton variant="secondary" @click="creating = false">Отмена</AppButton>
        <AppButton :loading="saving" @click="create">Добавить</AppButton>
      </template>
    </AppModal>
  </div>
</template>
