<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/client'
import { attachmentsApi, ordersApi } from '@/api/endpoints'
import AppButton from '@/components/ui/AppButton.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppTextarea from '@/components/ui/AppTextarea.vue'
import { formatFileSize } from '@/lib/format'
import { useToastStore } from '@/stores/toasts'

const router = useRouter()
const toasts = useToastStore()

const form = ref({
  deviceType: '',
  brand: '',
  model: '',
  serialNumber: '',
  problemDescription: '',
})

const files = ref<File[]>([])
const loading = ref(false)
const error = ref<ApiError | null>(null)

const MAX_SIZE = 10 * 1024 * 1024

function onFilesPicked(event: Event): void {
  const input = event.target as HTMLInputElement
  const picked = Array.from(input.files ?? [])

  const tooBig = picked.filter((file) => file.size > MAX_SIZE)
  if (tooBig.length > 0) {
    toasts.error(`Файл больше 10 МБ: ${tooBig.map((file) => file.name).join(', ')}`)
  }

  files.value = [...files.value, ...picked.filter((file) => file.size <= MAX_SIZE)].slice(0, 10)
  input.value = ''
}

function removeFile(index: number): void {
  files.value = files.value.filter((_, i) => i !== index)
}

async function submit(): Promise<void> {
  loading.value = true
  error.value = null

  try {
    const order = await ordersApi.create({
      deviceType: form.value.deviceType,
      brand: form.value.brand,
      model: form.value.model,
      serialNumber: form.value.serialNumber || null,
      problemDescription: form.value.problemDescription,
    })

    // Файлы прикладываются уже к созданной заявке — иначе их некуда положить.
    for (const file of files.value) {
      try {
        await attachmentsApi.upload(order.id, file)
      } catch {
        toasts.error(`Не удалось загрузить файл ${file.name}`)
      }
    }

    toasts.success(`Заявка ${order.number} создана`)
    await router.push({ name: 'order-details', params: { id: order.id } })
  } catch (exception) {
    error.value = exception instanceof ApiError ? exception : null
    toasts.error(error.value?.message ?? 'Не удалось создать заявку.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-6">
    <header>
      <h1 class="font-display text-2xl font-bold">Новая заявка</h1>
      <p class="mt-1 text-sm text-slate-500">
        Опишите поломку своими словами — мастер уточнит детали в комментариях.
      </p>
    </header>

    <form class="card space-y-4 p-6" @submit.prevent="submit">
      <div class="grid gap-4 sm:grid-cols-2">
        <AppInput
          v-model="form.deviceType"
          label="Тип устройства"
          placeholder="Ноутбук, смартфон, телевизор…"
          required
          :error="error?.fieldError('deviceType')"
        />
        <AppInput
          v-model="form.brand"
          label="Производитель"
          placeholder="Lenovo"
          required
          :error="error?.fieldError('brand')"
        />
        <AppInput
          v-model="form.model"
          label="Модель"
          placeholder="ThinkPad T14"
          required
          :error="error?.fieldError('model')"
        />
        <AppInput
          v-model="form.serialNumber"
          label="Серийный номер"
          hint="Необязательно"
          :error="error?.fieldError('serialNumber')"
        />
      </div>

      <AppTextarea
        v-model="form.problemDescription"
        label="Что случилось"
        placeholder="После падения не включается, при зарядке греется левый угол…"
        :rows="5"
        required
        :error="error?.fieldError('problemDescription')"
      />

      <div>
        <span class="field-label">Фотографии проблемы</span>

        <label
          class="flex cursor-pointer flex-col items-center gap-2 rounded-xl border-2 border-dashed border-slate-200 px-6 py-8 text-center transition-colors hover:border-brand-200 hover:bg-brand-50/40"
        >
          <svg class="size-6 text-slate-400" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M12 16V4m0 0L8 8m4-4 4 4M4 16v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2"
              stroke="currentColor"
              stroke-width="1.5"
              stroke-linecap="round"
            />
          </svg>
          <span class="text-sm text-slate-600">Перетащите или выберите файлы</span>
          <span class="text-xs text-slate-400">JPG, PNG, WebP или PDF, до 10 МБ</span>
          <input type="file" class="sr-only" multiple accept="image/*,.pdf" @change="onFilesPicked" />
        </label>

        <ul v-if="files.length" class="mt-3 space-y-2">
          <li
            v-for="(file, index) in files"
            :key="`${file.name}-${index}`"
            class="flex items-center justify-between gap-3 rounded-lg bg-slate-50 px-3 py-2 text-sm"
          >
            <span class="min-w-0 flex-1 truncate text-slate-700">{{ file.name }}</span>
            <span class="text-xs text-slate-400 tabular">{{ formatFileSize(file.size) }}</span>
            <button type="button" class="text-slate-400 hover:text-rose-600" @click="removeFile(index)">
              Убрать
            </button>
          </li>
        </ul>
      </div>

      <div class="flex justify-end gap-3 pt-2">
        <AppButton variant="secondary" @click="router.back()">Отмена</AppButton>
        <AppButton type="submit" :loading="loading">Создать заявку</AppButton>
      </div>
    </form>
  </div>
</template>
