<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { ApiError } from '@/api/client'
import { attachmentsApi } from '@/api/endpoints'
import type { AttachmentDto } from '@/api/types'
import AppButton from '@/components/ui/AppButton.vue'
import EmptyState from '@/components/ui/EmptyState.vue'
import SkeletonBlock from '@/components/ui/SkeletonBlock.vue'
import { formatFileSize, formatRelative } from '@/lib/format'
import { useToastStore } from '@/stores/toasts'

const props = defineProps<{ orderId: string; canUpload: boolean }>()

const toasts = useToastStore()

const files = ref<AttachmentDto[]>([])
const previews = ref<Record<string, string>>({})
const loading = ref(true)
const uploading = ref(false)

async function load(): Promise<void> {
  loading.value = true

  try {
    files.value = await attachmentsApi.list(props.orderId)

    // Файлы отдаются с проверкой прав, поэтому картинку нельзя просто вставить в <img src>:
    // тянем её с токеном и показываем как blob.
    for (const file of files.value.filter((item) => item.isImage)) {
      if (!previews.value[file.id]) {
        try {
          previews.value[file.id] = await attachmentsApi.blobUrl(file.id)
        } catch {
          // Не показали превью — не беда, ссылка на скачивание всё равно есть.
        }
      }
    }
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось загрузить файлы.')
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
      toasts.error(error instanceof ApiError ? error.message : `Не удалось загрузить ${file.name}`)
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
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось скачать файл.')
  }
}

async function remove(file: AttachmentDto): Promise<void> {
  try {
    await attachmentsApi.remove(file.id)
    await load()
  } catch (error) {
    toasts.error(error instanceof ApiError ? error.message : 'Не удалось удалить файл.')
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
  <div class="space-y-6">
    <SkeletonBlock v-if="loading" :lines="3" />

    <template v-else>
      <EmptyState
        v-if="files.length === 0"
        title="Файлов нет"
        description="Фотографии поломки и документы помогают мастеру быстрее понять проблему."
      />

      <ul v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <li v-for="file in files" :key="file.id" class="overflow-hidden rounded-xl border border-slate-200">
          <div class="flex h-36 items-center justify-center bg-slate-50">
            <img
              v-if="previews[file.id]"
              :src="previews[file.id]"
              :alt="file.fileName"
              class="h-full w-full object-cover"
            />
            <svg v-else class="size-8 text-slate-300" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M7 3h7l5 5v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z"
                stroke="currentColor"
                stroke-width="1.5"
              />
            </svg>
          </div>

          <div class="p-3">
            <p class="truncate text-sm font-medium text-slate-900" :title="file.fileName">
              {{ file.fileName }}
            </p>
            <p class="mt-0.5 text-xs text-slate-500">
              {{ formatFileSize(file.sizeBytes) }} · {{ file.uploadedBy.fullName }} ·
              {{ formatRelative(file.uploadedAt) }}
            </p>

            <div class="mt-2 flex gap-3 text-xs">
              <button class="text-brand-600 hover:text-brand-700" @click="download(file)">Скачать</button>
              <button class="text-slate-400 hover:text-rose-600" @click="remove(file)">Удалить</button>
            </div>
          </div>
        </li>
      </ul>

      <label
        v-if="canUpload"
        class="flex cursor-pointer items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-200 px-6 py-6 text-sm text-slate-500 transition-colors hover:border-brand-200 hover:bg-brand-50/40"
      >
        <span v-if="uploading">Загружаем…</span>
        <span v-else>Добавить файл — JPG, PNG, WebP или PDF до 10 МБ</span>
        <input type="file" class="sr-only" multiple accept="image/*,.pdf" @change="upload" />
      </label>

      <AppButton v-if="!canUpload && files.length" variant="ghost" size="sm" @click="load">
        Обновить список
      </AppButton>
    </template>
  </div>
</template>
