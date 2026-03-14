# GW2 Unlocks — Development Guide

This document describes how to run and develop the **GW2 Unlocks** project locally.

---

## Requirements

* **Node.js** (recommended: ≥ 18)
* **npm** (comes with Node)

Verify installation, install dependencies, and run the dev server all in one place:

```
node -v
npm -v
cd src/site/
npm install
npm run dev
```

This will start a local server at `http://localhost:5000` with hot reload and local filesystem access.

---

## Project Scripts (from package.json)

```
# Start the development server
npm run dev

# Build production bundle
npm run build

# Build and preview production bundle locally
npm run preview
```

---

## Development vs Production Behavior

* **Development mode** (`__VITE_MODE__ === "development"`):

  * Page title shows `[DEV]`
  * Dataset loaded from local file: `/src/data/data.json`

* **Production mode**:

  * Page title is normal
  * Dataset loaded from GitHub via jsDelivr using the COMMIT_SHA environment variable

---

## Notes

* The Vite dev server allows local access outside the project folder using:

```
// vite.config.js
server: {
  fs: {
    allow: [".."] // Only in development
  }
}
```

* Remember to set `VITE_COMMIT_SHA` for production builds if you want a specific jsDelivr URL.
