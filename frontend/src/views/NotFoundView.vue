<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppButton from '@/components/ui/AppButton.vue'

const router = useRouter()
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center">
    <p class="font-display text-6xl font-bold text-brand-600">404</p>
    <h1 class="font-display text-xl font-semibold">Такой страницы нет</h1>
    <p class="max-w-sm text-sm text-slate-500">
      Возможно, ссылка устарела или заявку уже закрыли.
    </p>
    <AppButton @click="router.push({ name: 'orders' })">К списку заявок</AppButton>
  </div>
</template>
