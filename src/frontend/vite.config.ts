import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The API runs on http://localhost:5180 (launch profile "http"); /api is proxied so cookies stay same-origin.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:5180', '/hubs': { target: 'http://localhost:5180', ws: true } },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
  },
})
