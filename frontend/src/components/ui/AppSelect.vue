<script setup lang="ts">
import { useId } from 'vue'

defineProps<{
  label?: string
  options: { value: string; label: string }[]
  placeholder?: string
  error?: string
  disabled?: boolean
}>()

const model = defineModel<string>({ default: '' })
const id = useId()
</script>

<template>
  <div>
    <label v-if="label" :for="id" class="field-label">{{ label }}</label>

    <select
      :id="id"
      v-model="model"
      :disabled="disabled"
      class="h-10 w-full rounded-lg border bg-white px-3 text-sm text-slate-900 disabled:bg-slate-50"
      :class="error ? 'border-rose-400' : 'border-slate-200'"
    >
      <option v-if="placeholder" value="">{{ placeholder }}</option>
      <option v-for="option in options" :key="option.value" :value="option.value">
        {{ option.label }}
      </option>
    </select>

    <p v-if="error" class="field-error">{{ error }}</p>
  </div>
</template>
