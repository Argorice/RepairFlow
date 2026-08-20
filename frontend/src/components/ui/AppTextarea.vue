<script setup lang="ts">
import { useId } from 'vue'

withDefaults(
  defineProps<{
    label?: string
    placeholder?: string
    error?: string
    hint?: string
    rows?: number
    required?: boolean
    disabled?: boolean
  }>(),
  { rows: 4 },
)

const model = defineModel<string>({ default: '' })
const id = useId()
</script>

<template>
  <div>
    <label v-if="label" :for="id" class="field-label">
      {{ label }}
      <span v-if="required" class="text-rose-500">*</span>
    </label>

    <textarea
      :id="id"
      v-model="model"
      :rows="rows"
      :placeholder="placeholder"
      :disabled="disabled"
      :aria-invalid="Boolean(error)"
      class="w-full resize-y rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 disabled:bg-slate-50"
      :class="error ? 'border-rose-400' : 'border-slate-200'"
    />

    <p v-if="error" class="field-error">{{ error }}</p>
    <p v-else-if="hint" class="mt-1.5 text-sm text-slate-500">{{ hint }}</p>
  </div>
</template>
