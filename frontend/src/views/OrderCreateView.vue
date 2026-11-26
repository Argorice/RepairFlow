<script setup lang="ts">
import { FileText, ImagePlus, X } from '@lucide/vue'
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/client'
import { attachmentsApi, ordersApi } from '@/api/endpoints'
import PageHeader from '@/components/layout/PageHeader.vue'
import AppButton from '@/components/ui/AppButton.vue'
import AppChip from '@/components/ui/AppChip.vue'
import AppInput from '@/components/ui/AppInput.vue'
import AppTextarea from '@/components/ui/AppTextarea.vue'
import { t, type MessageKey } from '@/i18n'
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

/** Самые частые типы устройств — одним касанием. Поле остаётся свободным: принесут что угодно. */
const DEVICE_TYPES: MessageKey[] = [
  'create.devices.laptop',
  'create.devices.phone',
  'create.devices.tablet',
  'create.devices.tv',
  'create.devices.console',
  'create.devices.printer',
]

interface PickedFile {
  file: File
  preview: string | null
}

const files = ref<PickedFile[]>([])
const loading = ref(false)
const error = ref<ApiError | null>(null)

const MAX_SIZE = 10 * 1024 * 1024
const MAX_FILES = 10
const MIN_DESCRIPTION = 10

/**
 * Те же правила, что и на сервере, но проверенные до отправки. Сервер остаётся источником
 * истины — здесь мы лишь избавляем человека от круга «отправил → подождал → получил отказ».
 */
const localErrors = ref<Record<string, string>>({})
const submitted = ref(false)

function validate(): boolean {
  const found: Record<string, string> = {}

  if (!form.value.deviceType.trim()) {
    found.deviceType = t('create.errors.deviceType')
  }

  if (!form.value.brand.trim()) {
    found.brand = t('create.errors.brand')
  }

  if (!form.value.model.trim()) {
    found.model = t('create.errors.model')
  }

  const description = form.value.problemDescription.trim()

  if (!description) {
    found.problemDescription = t('create.errors.problemEmpty')
  } else if (description.length < MIN_DESCRIPTION) {
    found.problemDescription = t('create.errors.problemShort', { count: MIN_DESCRIPTION })
  }

  localErrors.value = found
  return Object.keys(found).length === 0
}

/** Ошибка поля: сначала своя, потом пришедшая с сервера. */
function errorFor(field: keyof typeof form.value): string | undefined {
  return localErrors.value[field] ?? error.value?.fieldError(field)
}

// После первой неудачной отправки подсказки обновляются на лету, а не ждут следующего клика.
watch(
  form,
  () => {
    if (submitted.value) {
      validate()
    }
  },
  { deep: true },
)

const descriptionLength = computed(() => form.value.problemDescription.trim().length)

function onFilesPicked(event: Event): void {
  const input = event.target as HTMLInputElement
  const picked = Array.from(input.files ?? [])
  input.value = ''

  const tooBig = picked.filter((file) => file.size > MAX_SIZE)
  if (tooBig.length > 0) {
    toasts.error(t('create.fileTooBig', { names: tooBig.map((file) => file.name).join(', ') }))
  }

  const accepted = picked
    .filter((file) => file.size <= MAX_SIZE)
    .map((file) => ({
      file,
      preview: file.type.startsWith('image/') ? URL.createObjectURL(file) : null,
    }))

  files.value = [...files.value, ...accepted].slice(0, MAX_FILES)
}

function removeFile(index: number): void {
  const [removed] = files.value.splice(index, 1)
  if (removed?.preview) {
    URL.revokeObjectURL(removed.preview)
  }
}

async function submit(): Promise<void> {
  submitted.value = true

  if (!validate()) {
    toasts.error(t('create.checkFields'))

    // Ведём прямо к первому полю с ошибкой — на телефоне оно обычно за краем экрана.
    await nextTick()
    const invalid = document.querySelector<HTMLElement>('[aria-invalid="true"]')
    invalid?.scrollIntoView({ block: 'center', behavior: 'smooth' })
    invalid?.focus({ preventScroll: true })
    return
  }

  loading.value = true
  error.value = null

  try {
    const order = await ordersApi.create({
      deviceType: form.value.deviceType.trim(),
      brand: form.value.brand.trim(),
      model: form.value.model.trim(),
      serialNumber: form.value.serialNumber.trim() || null,
      problemDescription: form.value.problemDescription.trim(),
    })

    // Файлы прикладываются уже к созданной заявке — иначе их некуда положить.
    for (const { file } of files.value) {
      try {
        await attachmentsApi.upload(order.id, file)
      } catch {
        toasts.error(t('files.uploadFailed', { name: file.name }))
      }
    }

    toasts.success(t('create.created', { number: order.number }))
    await router.replace({ name: 'order-details', params: { id: order.id } })
  } catch (exception) {
    error.value = exception instanceof ApiError ? exception : null
    toasts.error(error.value?.message ?? t('create.failed'))
  } finally {
    loading.value = false
  }
}

onBeforeUnmount(() => {
  for (const item of files.value) {
    if (item.preview) {
      URL.revokeObjectURL(item.preview)
    }
  }
})
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-5">
    <PageHeader :title="t('create.title')" :description="t('create.subtitle')" />

    <form class="space-y-4" novalidate @submit.prevent="submit">
      <section class="card space-y-4 p-4 sm:p-6">
        <h2 class="section-title">{{ t('create.deviceSection') }}</h2>

        <div>
          <AppInput
            v-model="form.deviceType"
            :label="t('create.deviceType')"
            :placeholder="t('create.deviceTypePlaceholder')"
            required
            :error="errorFor('deviceType')"
          />
          <div class="-mx-4 mt-2.5 flex gap-2 overflow-x-auto px-4 scrollbar-none sm:mx-0 sm:flex-wrap sm:px-0">
            <AppChip
              v-for="key in DEVICE_TYPES"
              :key="key"
              :active="form.deviceType === t(key)"
              @click="form.deviceType = t(key)"
            >
              {{ t(key) }}
            </AppChip>
          </div>
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <AppInput
            v-model="form.brand"
            :label="t('create.brand')"
            placeholder="Lenovo"
            required
            :error="errorFor('brand')"
          />
          <AppInput
            v-model="form.model"
            :label="t('create.model')"
            placeholder="ThinkPad T14"
            required
            :error="errorFor('model')"
          />
        </div>

        <AppInput
          v-model="form.serialNumber"
          :label="t('create.serial')"
          :hint="t('common.optional')"
          :error="errorFor('serialNumber')"
        />
      </section>

      <section class="card space-y-4 p-4 sm:p-6">
        <div class="flex items-baseline justify-between gap-3">
          <h2 class="section-title">{{ t('create.problemSection') }}</h2>
          <span
            class="text-xs tabular"
            :class="descriptionLength >= MIN_DESCRIPTION ? 'text-success-ink' : 'text-ink-3'"
            aria-live="polite"
          >
            {{ descriptionLength }} / {{ MIN_DESCRIPTION }}+
          </span>
        </div>

        <AppTextarea
          v-model="form.problemDescription"
          :aria-label="t('create.problemSection')"
          :placeholder="t('create.problemPlaceholder')"
          :rows="5"
          required
          :hint="t('create.problemHint')"
          :error="errorFor('problemDescription')"
        />
      </section>

      <section class="card space-y-4 p-4 sm:p-6">
        <div>
          <h2 class="section-title">{{ t('create.photosSection') }}</h2>
          <p class="mt-0.5 text-sm text-ink-2">{{ t('create.photosHint') }}</p>
        </div>

        <ul v-if="files.length" class="grid grid-cols-3 gap-2 sm:grid-cols-4">
          <li
            v-for="(item, index) in files"
            :key="`${item.file.name}-${index}`"
            class="group relative aspect-square overflow-hidden rounded-xl bg-surface-2 ring-1 ring-line ring-inset"
          >
            <img v-if="item.preview" :src="item.preview" :alt="item.file.name" class="h-full w-full object-cover" />
            <div v-else class="flex h-full flex-col items-center justify-center gap-1 p-2 text-center">
              <FileText class="size-6 text-ink-3" aria-hidden="true" />
              <span class="w-full truncate text-[11px] text-ink-2">{{ item.file.name }}</span>
            </div>

            <span class="absolute bottom-1 left-1 rounded-md bg-black/55 px-1.5 py-0.5 text-[10px] text-white tabular">
              {{ formatFileSize(item.file.size) }}
            </span>

            <button
              type="button"
              class="pressable absolute top-1 right-1 flex size-7 items-center justify-center rounded-full bg-black/55 text-white hover:bg-black/70"
              :aria-label="t('create.removeFile', { name: item.file.name })"
              @click="removeFile(index)"
            >
              <X class="size-4" aria-hidden="true" />
            </button>
          </li>
        </ul>

        <label
          v-if="files.length < MAX_FILES"
          class="pressable flex cursor-pointer flex-col items-center gap-1.5 rounded-2xl border-2 border-dashed border-line px-6 py-7 text-center transition-colors hover:border-accent-line hover:bg-accent-soft/50 focus-within:border-accent"
        >
          <ImagePlus class="size-6 text-accent-ink" aria-hidden="true" />
          <span class="text-sm font-medium text-ink">{{ t('create.pickFiles') }}</span>
          <span class="text-xs text-ink-3">{{ t('files.limits') }}</span>
          <input type="file" class="sr-only" multiple accept="image/*,.pdf" @change="onFilesPicked" />
        </label>
      </section>

      <div class="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:justify-end sm:gap-3">
        <div class="hidden sm:block">
          <AppButton variant="ghost" size="lg" @click="router.back()">
            {{ t('common.cancel') }}
          </AppButton>
        </div>
        <AppButton type="submit" size="lg" :loading="loading" class="w-full sm:w-auto">
          {{ t('create.submit') }}
        </AppButton>
      </div>
    </form>
  </div>
</template>
