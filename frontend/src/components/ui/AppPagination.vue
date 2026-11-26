<script setup lang="ts">
import { ChevronLeft, ChevronRight } from '@lucide/vue'
import { t } from '@/i18n'
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
  <nav
    class="flex items-center justify-between gap-3 border-t border-line px-4 py-3"
    :aria-label="t('pagination.label')"
  >
    <p class="text-sm text-ink-3 tabular">
      {{ t('pagination.summary', { page, pages: Math.max(totalPages, 1), count: totalCount }) }}
    </p>

    <div class="flex gap-2">
      <AppButton
        variant="secondary"
        size="sm"
        square
        :icon="ChevronLeft"
        :label="t('pagination.previous')"
        :disabled="!hasPrevious"
        @click="emit('change', page - 1)"
      />
      <AppButton
        variant="secondary"
        size="sm"
        square
        :icon="ChevronRight"
        :label="t('pagination.next')"
        :disabled="!hasNext"
        @click="emit('change', page + 1)"
      />
    </div>
  </nav>
</template>
