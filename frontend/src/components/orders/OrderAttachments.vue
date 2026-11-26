<script setup lang="ts">
import { Download, FileText, ImagePlus, Paperclip, Trash2 } from '@lucide/vue'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { attachmentsApi } from '@/api/endpoints'
import type { AttachmentDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { t } from '@/i18n'
import { formatFileSize, formatRelative } from '@/lib/format'
import { useToastStore } from '@/stores/toasts'

const props = defineProps<{ orderId: string; canUpload: boolean }>()

const toasts = useToastStore()

const files = ref<AttachmentDto[]>([])
const previews = ref<Record<string, string>>({})
const loading = ref(true)
const uploading = ref(false)

async function load(): Promise<void> {
  try {
    files.value = await attachmentsApi.list(props.orderId)

    // Файлы отдаются с проверкой прав, поэтому картинку нельзя просто вставить в <img src>:
    // тянем её с токеном и показываем как blob.
    for (const file of files.value.filter((item) => item.isImage)) {
      if (!previews.value[file.id]) {
        try {
          previews.value[file.id] = await attachmentsApi.blobUrl(file.id)
        } catch {
          // Не показали превью — не беда, скачать файл всё равно можно.
        }
      }
    }
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('files.loadFailed'))
  } finally {
    loading.value = false
  }
}

async function upload(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const picked = Array.from(input.files ?? [])
  input.value = ''

  if (picked.length === 0) {
    return
  }

  uploading.value = true

  for (const file of picked) {
    try {
      await attachmentsApi.upload(props.orderId, file)
    } catch (error) {
      toasts.error(error instanceof ApiError ? error.message : t('files.uploadFailed', { name: file.name }))
    }
  }

  uploading.value = false
  await load()
}

async function download(file: AttachmentDto): Promise<void> {
  try {
    const url = previews.value[file.id] ?? (await attachmentsApi.blobUrl(file.id))
    const link = document.createElement('a')
    link.href = url
    link.download = file.fileName
    link.click()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('files.downloadFailed'))
  }
}

async function remove(file: AttachmentDto): Promise<void> {
  try {
    await attachmentsApi.remove(file.id)
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : t('files.removeFailed'))
  }
}

onBeforeUnmount(() => {
  for (const url of Object.values(previews.value)) {
    URL.revokeObjectURL(url)
  }
})

onMounted(load)
</script>

<template>
  <div class="space-y-5">
    <SkeletonBlock v-if="loading" :lines="3" />

    <template v-else>
      <EmptyState
        v-if="files.length === 0"
        :icon="Paperclip"
        :title="t('files.emptyTitle')"
        :description="t('files.emptyText')"
        compact
      />

      <ul v-else class="grid grid-cols-2 gap-3 lg:grid-cols-3">
        <li v-for="file in files" :key="file.id" class="overflow-hidden rounded-2xl bg-surface ring-1 ring-line ring-inset">
          <button
            type="button"
            class="flex aspect-[4/3] w-full items-center justify-center bg-surface-2"
            :aria-label="t('files.download', { name: file.fileName })"
            @click="download(file)"
          >
            <img
              v-if="previews[file.id]"
              :src="previews[file.id]"
              :alt="file.fileName"
              class="h-full w-full object-cover"
            />
            <FileText v-else class="size-8 text-ink-3" aria-hidden="true" />
          </button>

          <div class="flex items-start gap-1 p-3">
            <div class="min-w-0 flex-1">
              <p class="truncate text-sm font-medium text-ink" :title="file.fileName">{{ file.fileName }}</p>
              <p class="mt-0.5 truncate text-xs text-ink-3">
                {{ formatFileSize(file.sizeBytes) }} · {{ formatRelative(file.uploadedAt) }}
              </p>
            </div>

            <div class="-mt-1 -mr-1.5 flex">
              <AppButton
                variant="ghost"
                size="sm"
                square
                :icon="Download"
                :label="t('files.download', { name: file.fileName })"
                @click="download(file)"
              />
              <AppButton
                variant="ghost"
                size="sm"
                square
                :icon="Trash2"
                :label="t('common.delete')"
                @click="remove(file)"
              />
            </div>
          </div>
        </li>
      </ul>

      <label
        v-if="canUpload"
        class="pressable flex cursor-pointer flex-col items-center justify-center gap-1.5 rounded-2xl border-2 border-dashed border-line px-6 py-6 text-center transition-colors hover:border-accent-line hover:bg-accent-soft/50 focus-within:border-accent"
      >
        <ImagePlus class="size-6 text-accent-ink" aria-hidden="true" />
        <span class="text-sm font-medium text-ink">
          {{ uploading ? t('files.uploading') : t('files.add') }}
        </span>
        <span class="text-xs text-ink-3">{{ t('files.limits') }}</span>
        <input type="file" class="sr-only" multiple accept="image/*,.pdf" :disabled="uploading" @change="upload" />
      </label>
    </template>
  </div>
</template>
