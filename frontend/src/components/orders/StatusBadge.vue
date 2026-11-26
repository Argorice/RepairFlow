<script setup lang="ts">
import { Check } from '@lucide/vue'
import { computed } from 'vue'
import { OrderStatus } from '@/api/types'
import { statusLabel, statusStyle } from '@/lib/status'

const props = withDefaults(defineProps<{ status: OrderStatus; size?: 'sm' | 'md' }>(), { size: 'md' })

const style = computed(() => statusStyle(props.status))
</script>

<template>
  <span
    class="inline-flex shrink-0 items-center gap-1.5 rounded-full font-medium whitespace-nowrap ring-1 ring-inset"
    :class="[style.badge, size === 'sm' ? 'px-2 py-0.5 text-[11px]' : 'px-2.5 py-1 text-xs']"
  >
    <!-- Выданная заявка — единственная с галочкой: цвет «готово» и «выдано» близок, форма — нет -->
    <Check v-if="status === OrderStatus.Completed" class="size-3" stroke-width="3" aria-hidden="true" />
    <span v-else class="size-1.5 rounded-full" :class="style.solid" aria-hidden="true" />
    {{ statusLabel(status) }}
  </span>
</template>
