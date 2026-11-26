<script setup lang="ts">
import { Search, UserPlus, Users } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { usersApi } from '@/api/endpoints'
import { UserRole, type UserDto } from '@/api/types'
import PageHeader from '@/components/layout/PageHeader.vue'
import AppAvatar from '@/components/ui/AppAvatar.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppChip from '@/components/ui/AppChip.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppModal from '@/components/ui/AppModal.vue'
import AppSelect from '@/components/ui/AppSelect.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatDate } from '@/lib/format'
import { roleLabel } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const auth = useAuthStore()
const toasts = useToastStore()

const users = ref<UserDto[]>([])
const loading = ref(true)
const search = ref('')
const roleFilter = ref<UserRole | ''>('')

const creating = ref(false)
const saving = ref(false)
const createError = ref<ApiError | null>(null)

const emptyDraft = () => ({
  email: '',
  password: '',
  fullName: '',
  phone: '',
  role: UserRole.Technician as UserRole,
})

const draft = ref(emptyDraft())

const ROLES = Object.values(UserRole)

const roleOptions = computed(() => ROLES.map((value) => ({ value, label: roleLabel(value) })))
const staffRoleOptions = computed(() => roleOptions.value.filter((option) => option.value !== UserRole.Client))

async function load(): Promise<void> {
  try {
    users.value = await usersApi.list({
      role: roleFilter.value || undefined,
      search: search.value || undefined,
    })
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('users.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function changeRole(user: UserDto, role: UserRole): Promise<void> {
  try {
    const updated = await usersApi.update(user.id, { role })
    users.value = users.value.map((item) => (item.id === updated.id ? updated : item))
    toasts.success(t('users.roleChanged', { name: updated.fullName, role: roleLabel(updated.role) }))
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('users.roleFailed'))
    await load()
  }
}

async function toggleActive(user: UserDto): Promise<void> {
  try {
    const updated = await usersApi.update(user.id, { isActive: !user.isActive })
    users.value = users.value.map((item) => (item.id === updated.id ? updated : item))
    toasts.success(updated.isActive ? t('users.enabled') : t('users.disabled'))
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('users.statusFailed'))
  }
}

function openCreate(): void {
  draft.value = emptyDraft()
  createError.value = null
  creating.value = true
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
    toasts.success(t('users.created'))
    await load()
  } catch (exception) {
    createError.value = exception instanceof ApiError ? exception : null
    toasts.error(createError.value?.message ?? t('users.createFailed'))
  } finally {
    saving.value = false
  }
}

let debounce: number | undefined

function reloadSoon(): void {
  window.clearTimeout(debounce)
  debounce = window.setTimeout(load, 250)
}

function setRole(role: UserRole | ''): void {
  roleFilter.value = role
  void load()
}

onMounted(load)
</script>

<template>
  <div class="space-y-5">
    <PageHeader :title="t('nav.users')" :description="t('users.subtitle')">
      <template #actions>
        <div class="hidden lg:block">
          <AppButton :icon="UserPlus" @click="openCreate">{{ t('users.add') }}</AppButton>
        </div>
      </template>
    </PageHeader>

    <!-- На телефоне кнопка добавления живёт в верхней панели -->
    <Teleport to="#appbar-actions" defer>
      <AppButton variant="ghost" square :icon="UserPlus" :label="t('users.add')" class="text-accent-ink" @click="openCreate" />
    </Teleport>

    <div class="space-y-3">
      <AppInput
        v-model="search"
        type="search"
        inputmode="search"
        :icon="Search"
        :placeholder="t('users.searchPlaceholder')"
        :aria-label="t('users.search')"
        @update:model-value="reloadSoon"
      />

      <div class="-mx-4 flex gap-2 overflow-x-auto px-4 scrollbar-none sm:mx-0 sm:px-0" role="group" :aria-label="t('users.roleFilter')">
        <AppChip :active="roleFilter === ''" @click="setRole('')">{{ t('users.allRoles') }}</AppChip>
        <AppChip v-for="role in ROLES" :key="role" :active="roleFilter === role" @click="setRole(role)">
          {{ roleLabel(role) }}
        </AppChip>
      </div>
    </div>

    <div class="card overflow-hidden">
      <SkeletonBlock v-if="loading" variant="rows" :lines="6" />

      <EmptyState v-else-if="users.length === 0" :icon="Users" :title="t('users.emptyTitle')" :description="t('users.emptyText')" />

      <ul v-else class="divide-y divide-line">
        <li
          v-for="user in users"
          :key="user.id"
          class="grid grid-cols-[auto_minmax(0,1fr)] items-center gap-x-3 gap-y-3 px-4 py-4 sm:grid-cols-[auto_minmax(0,1fr)_10rem_auto] sm:gap-x-4"
        >
          <AppAvatar :name="user.fullName" :tone="user.isActive ? 'accent' : 'neutral'" />

          <div class="min-w-0" :class="user.isActive ? '' : 'opacity-60'">
            <p class="flex items-center gap-2 truncate font-medium text-ink">
              <span class="truncate">{{ user.fullName }}</span>
              <span v-if="user.id === auth.user?.id" class="shrink-0 rounded-full bg-surface-2 px-2 py-0.5 text-[11px] text-ink-2">
                {{ t('users.you') }}
              </span>
              <span v-if="!user.isActive" class="shrink-0 rounded-full bg-neutral-soft px-2 py-0.5 text-[11px] text-neutral-ink">
                {{ t('users.inactive') }}
              </span>
            </p>
            <p class="truncate text-sm text-ink-2">{{ user.email }}</p>
            <p class="truncate text-xs text-ink-3">
              {{ user.phone ?? t('users.noPhone') }} · {{ t('users.since', { date: formatDate(user.createdAt) }) }}
            </p>
          </div>

          <!-- На телефоне управление уходит второй строкой под имя -->
          <div class="col-span-2 flex items-center gap-2 sm:col-span-2 sm:contents">
            <AppSelect
              class="flex-1 sm:flex-none"
              :model-value="user.role"
              :options="roleOptions"
              :aria-label="t('users.roleFor', { name: user.fullName })"
              :disabled="user.id === auth.user?.id"
              @update:model-value="(value) => changeRole(user, value as UserRole)"
            />

            <AppButton
              :variant="user.isActive ? 'ghost' : 'soft'"
              size="sm"
              :disabled="user.id === auth.user?.id"
              @click="toggleActive(user)"
            >
              {{ user.isActive ? t('users.disable') : t('users.enable') }}
            </AppButton>
          </div>
        </li>
      </ul>
    </div>

    <AppModal v-if="creating" :title="t('users.newTitle')" :description="t('users.newText')" @close="creating = false">
      <form id="create-user" class="space-y-4" @submit.prevent="create">
        <AppInput v-model="draft.fullName" :label="t('fields.fullName')" required :error="createError?.fieldError('fullName')" />
        <AppInput
          v-model="draft.email"
          :label="t('fields.email')"
          type="email"
          inputmode="email"
          required
          :error="createError?.fieldError('email')"
        />
        <AppInput
          v-model="draft.password"
          :label="t('fields.password')"
          type="password"
          autocomplete="new-password"
          required
          :hint="t('fields.passwordHint')"
          :error="createError?.fieldError('password')"
        />
        <AppInput v-model="draft.phone" :label="t('fields.phone')" type="tel" inputmode="tel" />
        <AppSelect v-model="draft.role" :label="t('fields.role')" :options="staffRoleOptions" />
      </form>

      <template #actions>
        <AppButton variant="secondary" @click="creating = false">{{ t('common.cancel') }}</AppButton>
        <AppButton type="submit" form="create-user" :loading="saving">{{ t('common.add') }}</AppButton>
      </template>
    </AppModal>
  </div>
</template>
