# GW2 Unlocks

## Development Guide

This document describes how to run and develop the **GW2 Unlocks** project locally.

---

### Requirements

* **Node.js** (recommended: ≥ 18)
* **npm** (comes with Node)

Verify installation, install dependencies, and run the dev server:

```bash
node -v
npm -v
cd src/site/
npm install
npm run dev
```

This will start a local server at `http://localhost:5000` with hot reload.

---

### Project Scripts (from package.json)

```bash
# Start the development server
npm run dev

# Build production bundle
npm run build

# Build and preview production bundle locally
npm run preview
```

---

### Development and Production Behavior

* **Dataset** is served from `public/data/data.json`.

  * Works in both development (`vite dev`) and production (`vite build` → Netlify).
  * Fetch in code:

```ts
const DATASET_URL = '/data/data.json';
const res = await fetch(DATASET_URL);
const unlockData = await res.json();
```

* **Page title**:

  * `[DEV]` is appended in development mode.
  * Normal title in production.

---

### Notes

* Static assets in `public/` are copied into the build output (`dist/`) automatically.
* No external CDN or jsDelivr fetch is required; all data is served locally and via Netlify’s CDN in production.
* `vite.config.js` only configures the front-end build — no changes are needed for Node scripts or other jobs.
