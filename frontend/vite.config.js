import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwindcss from "@tailwindcss/vite";

import fs from "node:fs";

const developmentCertificateName = "localhost.pfx";

const httpsSettings = fs.existsSync(developmentCertificateName)
  ? {
      pfx: fs.readFileSync(developmentCertificateName),
      passphrase: "YOUR_ACTUAL_PASSPHRASE_HERE",
    }
  : {};

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    https: httpsSettings,
  },
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
});
