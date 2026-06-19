import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/searches': 'http://localhost:5000',
      '/listings': 'http://localhost:5000',
      '/health': 'http://localhost:5000',
    },
  },
})
