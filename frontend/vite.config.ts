import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        port: 3000,
        proxy: {
            '/api': {
                target: 'http://127.0.0.1:5200', // Backend chạy ở port 5200
                changeOrigin: true,
                secure: false,
            }
        }
    }
})
