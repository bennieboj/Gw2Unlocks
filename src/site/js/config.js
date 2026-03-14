export const DATASET_URL = (() => {
  if (window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1") {
    return "data/data.json"; // local copy
  }  
  else if (__COMMIT_SHA__) {
    // Production: COMMIT_SHA comes from Vite env
    return `https://cdn.jsdelivr.net/gh/bennieboj/Gw2Unlocks@${__COMMIT_SHA__}/src/data/data.json`;
  }
  else {
    throw new Error("DATASET_URL could not be determined.");
  }
})();