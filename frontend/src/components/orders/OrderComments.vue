<script setup lang="ts">
import { Lock, MessageSquare, SendHorizontal } from '@lucide/vue'
import { nextTick, onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { commentsApi } from '@/api/endpoints'
import type { CommentDto } from '@/api/types'
import AppAvatar from '@/components/ui/AppAvatar.vue'
import AppButton from '@/components/ui/AppButton.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatRelative } from '@/lib/format'
import { roleLabel } from '@/lib/status'
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
const field = ref<HTMLTextAreaElement | null>(null)

async function load(): Promise<void> {
  try {
    comments.value = await commentsApi.list(props.orderId)
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('comments.loadFailed'))
  } finally {
    loading.value = false
  }
}

/** Поле растёт вместе с текстом, как в мессенджере, но не выше шести строк. */
function autoGrow(): void {
  const element = field.value
  if (!element) {
    return
  }

  element.style.height = 'auto'
  element.style.height = `${Math.min(element.scrollHeight, 160)}px`
}

async function send(): Promise<void> {
  if (!text.value.trim()) {
    return
  }

  sending.value = true

  try {
    const created = await commentsApi.add(props.orderId, text.value.trim(), isInternal.value)
    comments.value = [...comments.value, created]
    text.value = ''
    await nextTick()
    autoGrow()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('comments.sendFailed'))
  } finally {
    sending.value = false
  }
}

/** Ctrl/⌘ + Enter отправляет; просто Enter — новая строка, чтобы не отправить полуфразу. */
function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
    event.preventDefault()
    void send()
  }
}

const isMine = (comment: CommentDto): boolean => comment.author.id === auth.user?.id

defineExpose({ reload: load })

onMounted(load)
</script>

<template>
  <div class="space-y-5">
    <SkeletonBlock v-if="loading" :lines="4" />

    <EmptyState
      v-else-if="comments.length === 0"
      :icon="MessageSquare"
      :title="t('comments.emptyTitle')"
      :description="t('comments.emptyText')"
      compact
    />

    <ul v-else class="space-y-4">
      <li
        v-for="comment in comments"
        :key="comment.id"
        class="flex gap-2.5"
        :class="isMine(comment) ? 'flex-row-reverse' : ''"
      >
        <AppAvatar
          v-if="!isMine(comment)"
          :name="comment.author.fullName"
          size="sm"
          :tone="comment.isInternal ? 'warn' : 'neutral'"
        />

        <div class="flex max-w-[85%] min-w-0 flex-col sm:max-w-[75%]" :class="isMine(comment) ? 'items-end' : 'items-start'">
          <div class="mb-1 flex flex-wrap items-baseline gap-x-1.5 px-1 text-xs text-ink-3">
            <span v-if="!isMine(comment)" class="font-medium text-ink-2">{{ comment.author.fullName }}</span>
            <span v-if="!isMine(comment)">{{ roleLabel(comment.author.role) }} ·</span>
            <span>{{ formatRelative(comment.createdAt) }}</span>
          </div>

          <div
            class="rounded-2xl px-3.5 py-2.5 text-sm leading-relaxed whitespace-pre-line"
            :class="[
              comment.isInternal
                ? 'bg-warn-soft text-ink ring-1 ring-warn-line ring-inset'
                : isMine(comment)
                  ? 'bg-accent text-on-accent'
                  : 'bg-surface-2 text-ink',
              isMine(comment) ? 'rounded-tr-md' : 'rounded-tl-md',
            ]"
          >
            <p
              v-if="comment.isInternal"
              class="mb-1 flex items-center gap-1 text-[11px] font-semibold tracking-wide text-warn-ink uppercase"
            >
              <Lock class="size-3" aria-hidden="true" />
              {{ t('comments.internalBadge') }}
            </p>
            {{ comment.text }}
          </div>
        </div>
      </li>
    </ul>

    <form
      class="rounded-2xl bg-surface p-2 ring-1 ring-line transition-shadow ring-inset focus-within:ring-2 focus-within:ring-accent"
      @submit.prevent="send"
    >
      <label for="comment-text" class="sr-only">{{ t('comments.placeholder') }}</label>
      <textarea
        id="comment-text"
        ref="field"
        v-model="text"
        rows="2"
        :placeholder="isInternal ? t('comments.placeholderInternal') : t('comments.placeholder')"
        class="block max-h-40 w-full resize-none bg-transparent px-2 py-1.5 text-base text-ink placeholder:text-ink-3 focus:outline-none sm:text-sm"
        @input="autoGrow"
        @keydown="onKeydown"
      />

      <div class="flex items-center justify-between gap-2 pt-1">
        <button
          v-if="auth.isStaff"
          type="button"
          :aria-pressed="isInternal"
          class="pressable inline-flex h-8 items-center gap-1.5 rounded-full px-3 text-xs font-medium ring-1 transition-colors ring-inset"
          :class="isInternal ? 'bg-warn-soft text-warn-ink ring-warn-line' : 'text-ink-2 ring-line hover:text-ink'"
          @click="isInternal = !isInternal"
        >
          <Lock class="size-3.5" aria-hidden="true" />
          {{ t('comments.internalToggle') }}
        </button>
        <span v-else />

        <AppButton
          type="submit"
          size="sm"
          :icon="SendHorizontal"
          :loading="sending"
          :disabled="!text.trim()"
        >
          {{ t('comments.send') }}
        </AppButton>
      </div>
    </form>

    <p v-if="isInternal" class="-mt-3 px-1 text-xs text-warn-ink">{{ t('comments.internalHint') }}</p>
  </div>
</template>
