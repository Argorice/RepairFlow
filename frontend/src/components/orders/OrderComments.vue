<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { commentsApi } from '@/api/endpoints'
import type { CommentDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import AppTextarea from '@/components/ui/AppTextarea.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatRelative, initials } from '@/lib/format'
import { ROLE_LABELS } from '@/lib/status'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toasts'

const props = defineProps<{ orderId: string }>()

const auth = useAuthStore()
const toasts = useToastStore()

const comments = ref<CommentDto[]>([])
const loading = ref(true)
const sending = ref(false)
const text = ref('')
const isInternal = ref(false)

async function load(): Promise<void> {
  loading.value = true

  try {
    comments.value = await commentsApi.list(props.orderId)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить переписку.')
  } finally {
    loading.value = false
  }
}

async function send(): Promise<void> {
  if (!text.value.trim()) {
    return
  }

  sending.value = true

  try {
    const created = await commentsApi.add(props.orderId, text.value, isInternal.value)
    comments.value = [...comments.value, created]
    text.value = ''
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось отправить сообщение.')
  } finally {
    sending.value = false
  }
}

defineExpose({ reload: load })

onMounted(load)
</script>

<template>
  <div class="space-y-6">
    <SkeletonBlock v-if="loading" :lines="4" />

    <EmptyState
      v-else-if="comments.length === 0"
      title="Сообщений пока нет"
      description="Здесь мастер и клиент обсуждают ремонт. Напишите первым."
    />

    <ul v-else class="space-y-4">
      <li v-for="comment in comments" :key="comment.id" class="flex gap-3">
        <span
          class="flex size-9 shrink-0 items-center justify-center rounded-full text-xs font-semibold"
          :class="comment.isInternal ? 'bg-amber-100 text-amber-800' : 'bg-slate-100 text-slate-600'"
        >
          {{ initials(comment.author.fullName) }}
        </span>

        <div class="min-w-0 flex-1">
          <div class="flex flex-wrap items-baseline gap-x-2">
            <span class="text-sm font-medium text-slate-900">{{ comment.author.fullName }}</span>
            <span class="text-xs text-slate-400">{{ ROLE_LABELS[comment.author.role] }}</span>
            <span class="text-xs text-slate-400">· {{ formatRelative(comment.createdAt) }}</span>
            <span
              v-if="comment.isInternal"
              class="rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-medium text-amber-800"
            >
              внутренняя заметка
            </span>
          </div>

          <p
            class="mt-1 rounded-xl px-3 py-2 text-sm whitespace-pre-line"
            :class="comment.isInternal ? 'bg-amber-50 text-amber-900' : 'bg-slate-50 text-slate-700'"
          >
            {{ comment.text }}
          </p>
        </div>
      </li>
    </ul>

    <form class="rounded-xl border border-slate-200 p-4" @submit.prevent="send">
      <AppTextarea v-model="text" :rows="3" placeholder="Написать сообщение…" />

      <div class="mt-3 flex flex-wrap items-center justify-between gap-3">
        <label v-if="auth.isStaff" class="flex items-center gap-2 text-sm text-slate-600">
          <input v-model="isInternal" type="checkbox" class="size-4 rounded border-slate-300 text-brand-600" />
          Внутренняя заметка — клиент её не увидит
        </label>
        <span v-else />

        <AppButton type="submit" :loading="sending" :disabled="!text.trim()">Отправить</AppButton>
      </div>
    </form>
  </div>
</template>
