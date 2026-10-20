import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: { outDir: '../backend/Atlas.Web/wwwroot', emptyOutDir: true },
  server: {
    host: '127.0.0.1',
    port: 5173,
    proxy: { '/api': { target: 'http://localhost:5166', changeOrigin: true } }
  }
});
