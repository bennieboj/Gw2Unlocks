import { DATASET_URL } from "./config.ts";
import { UnlockItem } from "./unlock-item.ts";

const STORAGE_KEYS = {
  apiKey: "gw2_api_key",
  minis: "gw2_account_minis",
  skins: "gw2_account_skins",
  lastRefresh: "gw2_last_refresh"
} as const;

type StorageKey = keyof typeof STORAGE_KEYS;

const REFRESH_COOLDOWN = 5 * 60 * 1000; // 5 minutes

class Gw2Unlocks extends HTMLElement {
  unlockData: UnlockItem[] = [];

  // Connected callback
  async connectedCallback() {
    console.log(`using mode ${__VITE_MODE__}, dataset ${DATASET_URL}`);
    document.title = "GW2 Unlocks" + (__VITE_MODE__ === "development" ? " [DEV]" : "");

    try {
      const res = await fetch(DATASET_URL);
      this.unlockData = await res.json() as UnlockItem[];
    } catch (e) {
      console.error("Failed to load dataset", e);
      this.unlockData = [];
    }

    this.render();
    this.renderItems();
  }

  // Render main HTML
  render() {
    const apiKey = localStorage.getItem(STORAGE_KEYS.apiKey) || "";

    this.innerHTML = `
      <div class="controls">
        API Key:
        <input id="apikey" value="${apiKey}" size="60">
        <button id="savekey">Save</button>
        <button id="refresh">Refresh API Data</button>
        <span id="status"></span>
      </div>
      <h2>Minis</h2>
      <div id="minis" class="group"></div>
      <h2>Skins</h2>
      <div id="skins" class="group"></div>
    `;

    this.querySelector<HTMLButtonElement>("#savekey")!.onclick = () => this.saveKey();
    this.querySelector<HTMLButtonElement>("#refresh")!.onclick = () => this.refreshApi();
  }

  // Save API key
  saveKey() {
    const keyInput = this.querySelector<HTMLInputElement>("#apikey")!;
    const key = keyInput.value.trim();
    localStorage.setItem(STORAGE_KEYS.apiKey, key);
    this.setStatus("API key saved");
  }

  // Display status
  setStatus(msg: string) {
    const statusEl = this.querySelector<HTMLSpanElement>("#status")!;
    statusEl.innerText = msg;
  }

  // Get stored unlocks from localStorage
  getStoredUnlocks(type: "minis" | "skins"): number[] {
    const data = localStorage.getItem(STORAGE_KEYS[type]);
    return data ? JSON.parse(data) : [];
  }

  // Render unlock items
  renderItems() {
    const minisUnlocked = this.getStoredUnlocks("minis");
    const skinsUnlocked = this.getStoredUnlocks("skins");

    const minisContainer = this.querySelector<HTMLDivElement>("#minis")!;
    const skinsContainer = this.querySelector<HTMLDivElement>("#skins")!;

    const minis = this.unlockData.filter(i => i.unlock_type === "minis");
    const skins = this.unlockData.filter(i => i.unlock_type === "skins");

    minisContainer.innerHTML = minis.map(item => `
      <div class="item ${minisUnlocked.includes(item.unlock_typeid) ? "unlocked" : ""}">
        <img src="${item.icon}" alt="${item.name}">
        <div>${item.name}</div>
      </div>
    `).join("");

    skinsContainer.innerHTML = skins.map(item => `
      <div class="item ${skinsUnlocked.includes(item.unlock_typeid) ? "unlocked" : ""}">
        <img src="${item.icon}" alt="${item.name}">
        <div>${item.name}</div>
      </div>
    `).join("");
  }

  // Refresh API data
  async refreshApi() {
    const last = localStorage.getItem(STORAGE_KEYS.lastRefresh);
    if (last && Date.now() - parseInt(last) < REFRESH_COOLDOWN) {
      const remaining = Math.ceil((REFRESH_COOLDOWN - (Date.now() - parseInt(last))) / 1000);
      this.setStatus(`Please wait ${remaining}s before refreshing again`);
      return;
    }

    const apiKey = localStorage.getItem(STORAGE_KEYS.apiKey);
    if (!apiKey) {
      this.setStatus("No API key set");
      return;
    }

    this.setStatus("Loading API data...");

    try {
      const [minisRes, skinsRes] = await Promise.all([
        fetch(`https://api.guildwars2.com/v2/account/minis?access_token=${apiKey}`),
        fetch(`https://api.guildwars2.com/v2/account/skins?access_token=${apiKey}`)
      ]);

      const minis: number[] = await minisRes.json();
      const skins: number[] = await skinsRes.json();

      localStorage.setItem(STORAGE_KEYS.minis, JSON.stringify(minis));
      localStorage.setItem(STORAGE_KEYS.skins, JSON.stringify(skins));
      localStorage.setItem(STORAGE_KEYS.lastRefresh, Date.now().toString());

      this.setStatus("API data refreshed");
      this.renderItems();
    } catch (e) {
      console.error("API error", e);
      this.setStatus("API error");
    }
  }
}

customElements.define("gw2-unlocks", Gw2Unlocks);
