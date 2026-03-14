import { defineConfig } from 'vite'


export default defineConfig(({ mode }) => {
  return {
    // Only enable fs.allow when running the dev server
    server: {
      fs: {
        allow: mode === "development" ? [".."] : []
      }
    },
    define: {
      __COMMIT_SHA__: JSON.stringify(process.env.COMMIT_REF || ""),
      __VITE_MODE__: JSON.stringify(mode)
    }
  };
});