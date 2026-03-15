import { defineConfig } from 'vite'


export default defineConfig(({ mode }) => {
  return {
    define: {
      __VITE_MODE__: JSON.stringify(mode)
    }
  };
});