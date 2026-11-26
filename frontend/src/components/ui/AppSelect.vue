<script setup lang="ts">
import { ChevronDown } from '@lucide/vue'
import { useId } from 'vue'

defineProps<{
  label?: string
  options: { value: string; label: string }[]
  placeholder?: string
  error?: string
  disabled?: boolean
  ariaLabel?: string
}>()

const model = defineModel<string>({ default: '' })
const id = useId()
</script>

<template>
  <div>
    <label v-if="label" :for="id" class="field-label">{{ label }}</label>

    <!--
      Нативный select, а не самописный выпадающий список: на телефоне он открывает системное
      колесо выбора — привычное, доступное и без единой строчки кода на клавиатурную навигацию.
    -->
    <div class="relative">
      <select
        :id="id"
        v-model="model"
        :disabled="disabled"
        :aria-label="ariaLabel"
        :aria-invalid="Boolean(error)"
        class="control cursor-pointer appearance-none pr-10"
        :class="model === '' && placeholder ? 'text-ink-3' : ''"
      >
        <option v-if="placeholder !== undefined" value="">{{ placeholder }}</option>
        <option v-for="option in options" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>

      <ChevronDown
        class="pointer-events-none absolute top-1/2 right-3.5 size-4 -translate-y-1/2 text-ink-3"
        aria-hidden="true"
      />
    </div>

    <p v-if="error" class="field-error">{{ error }}</p>
  </div>
</template>
