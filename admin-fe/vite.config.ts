import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    // Cổng này phải khớp với Cors:AllowedOrigins ở backend.
    port: 5173,
    strictPort: true,
  },
})
