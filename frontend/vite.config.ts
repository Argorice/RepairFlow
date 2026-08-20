import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    // В разработке повторяем то же, что в продакшне делает Vercel: API живёт на том же
    // origin, что и приложение. Тогда кука с refresh-токеном везде остаётся first-party.
    proxy: {
      '/api': { target: 'http://localhost:5080', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5080', changeOrigin: true, ws: true },
      '/health': { target: 'http://localhost:5080', changeOrigin: true },
    },
  },
})
