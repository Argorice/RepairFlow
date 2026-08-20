<script setup lang="ts">
import { useId } from 'vue'

withDefaults(
  defineProps<{
    label?: string
    type?: string
    placeholder?: string
    error?: string
    hint?: string
    required?: boolean
    disabled?: boolean
    autocomplete?: string
  }>(),
  { type: 'text' },
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

    <input
      :id="id"
      v-model="model"
      :type="type"
      :placeholder="placeholder"
      :disabled="disabled"
      :autocomplete="autocomplete"
      :aria-invalid="Boolean(error)"
      :aria-describedby="error ? `${id}-error` : undefined"
      class="h-10 w-full rounded-lg border bg-white px-3 text-sm text-slate-900 placeholder:text-slate-400 disabled:bg-slate-50"
      :class="error ? 'border-rose-400' : 'border-slate-200'"
    />

    <p v-if="error" :id="`${id}-error`" class="field-error">{{ error }}</p>
    <p v-else-if="hint" class="mt-1.5 text-sm text-slate-500">{{ hint }}</p>
  </div>
</template>
