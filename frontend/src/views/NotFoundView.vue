<script setup lang="ts">
import { useRouter } from 'vue-router'
import BrandMark from '@/components/layout/BrandMark.vue'
import AppButton from '@/components/ui/AppButton.vue'
import { t } from '@/i18n'

const router = useRouter()
</script>

<template>
  <div class="flex min-h-dvh flex-col items-center justify-center gap-4 bg-canvas px-6 text-center">
    <BrandMark compact />
    <p class="font-display text-7xl font-extrabold text-accent-ink">404</p>
    <h1 class="font-display text-xl font-semibold">{{ t('notFound.title') }}</h1>
    <p class="max-w-sm text-sm text-ink-2">{{ t('notFound.text') }}</p>
    <AppButton size="lg" @click="router.push({ name: 'orders' })">{{ t('notFound.back') }}</AppButton>
  </div>
</template>
