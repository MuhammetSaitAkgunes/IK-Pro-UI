import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Backend http profili (launchSettings.json): TLS sertifika derdi olmadan geliştirme.
      "/api": { target: "http://localhost:5053", changeOrigin: true },
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/test/setup.ts",
    alias: {
      // jsdom'da gerçek canvas yok; grafikler test ortamında stub'lanır.
      "react-chartjs-2": fileURLToPath(new URL("./src/test/chartStub.tsx", import.meta.url)),
    },
  },
});
