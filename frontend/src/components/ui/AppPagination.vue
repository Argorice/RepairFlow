<script setup lang="ts">
import AppButton from './AppButton.vue'

defineProps<{
  page: number
  totalPages: number
  totalCount: number
  hasPrevious: boolean
  hasNext: boolean
}>()

const emit = defineEmits<{ change: [page: number] }>()
</script>

<template>
  <div class="flex flex-col items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 sm:flex-row">
    <p class="text-sm text-slate-500">
      Страница {{ page }} из {{ Math.max(totalPages, 1) }} · всего {{ totalCount }}
    </p>

    <div class="flex gap-2">
      <AppButton variant="secondary" size="sm" :disabled="!hasPrevious" @click="emit('change', page - 1)">
        Назад
      </AppButton>
      <AppButton variant="secondary" size="sm" :disabled="!hasNext" @click="emit('change', page + 1)">
        Вперёд
      </AppButton>
    </div>
  </div>
</template>
