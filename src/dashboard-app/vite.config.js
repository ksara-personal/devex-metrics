import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// See README.md for how to point the dev server's proxy at your local
// DevExMetrics API if you'd rather not rely on server-side CORS at all.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Uncomment and adjust if you want Vite to proxy /api-> your local
    // DevExMetrics service instead of hitting it directly from the browser.
    // proxy: {
    //   "/api": {
    //     target: "http://localhost:5100",
    //     changeOrigin: true,
    //     rewrite: (path) => path.replace(/^\/api/, ""),
    //   },
    // },
  },
});
