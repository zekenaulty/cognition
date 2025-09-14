import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // Example API proxy during dev
      '/api': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      },
      '/openapi': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      },
      '/users': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      },
      '/personas': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      },
      '/hangfire': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      },
      '/swagger': {
        target: process.env.VITE_API_BASE_URL || 'http://localhost:5253',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: resolve(__dirname, '../Cognition.Api/wwwroot'),
    emptyOutDir: true
  }
}))
