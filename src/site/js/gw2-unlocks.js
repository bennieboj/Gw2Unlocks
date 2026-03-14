import { DATASET_URL } from "./config.js";

const STORAGE_KEYS = {
  apiKey: "gw2_api_key",
  minis: "gw2_account_minis",
  skins: "gw2_account_skins",
  lastRefresh: "gw2_last_refresh"
};
const REFRESH_COOLDOWN = 5 * 60 * 1000;

class Gw2Unlocks extends HTMLElement {
  async connectedCallback() {
    console.log(`using mode ${__VITE_MODE__} ${DATASET_URL}`);

    document.title = "GW2 Unlocks" + (__VITE_MODE__ === "development" ? " [DEV]" : "");

    const res = await fetch(DATASET_URL);
    this.unlockData = await res.json();

    this.render();
    this.renderItems();
  }

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

    this.querySelector("#savekey").onclick = () => this.saveKey();
    this.querySelector("#refresh").onclick = () => this.refreshApi();
  }

  saveKey() {
    const key = this.querySelector("#apikey").value.trim();
    localStorage.setItem(STORAGE_KEYS.apiKey, key);
    this.setStatus("API key saved");
  }

  setStatus(msg) {
    this.querySelector("#status").innerText = msg;
  }

  getStoredUnlocks(type) {
    const data = localStorage.getItem(STORAGE_KEYS[type]);
    return data ? JSON.parse(data) : [];
  }

  renderItems() {
    const minisUnlocked = this.getStoredUnlocks("minis");
    const skinsUnlocked = this.getStoredUnlocks("skins");

    const minis = this.unlockData.filter(i => i.unlock_type === "minis");
    const skins = this.unlockData.filter(i => i.unlock_type === "skins");

    this.querySelector("#minis").innerHTML = minis.map(item => `
      <div class="item ${minisUnlocked.includes(item.unlock_typeid) ? "unlocked" : ""}">
        <img src="${item.icon}">
        <div>${item.name}</div>
      </div>
    `).join("");

    this.querySelector("#skins").innerHTML = skins.map(item => `
      <div class="item ${skinsUnlocked.includes(item.unlock_typeid) ? "unlocked" : ""}">
        <img src="${item.icon}">
        <div>${item.name}</div>
      </div>
    `).join("");
  }

  async refreshApi() {
    const last = localStorage.getItem(STORAGE_KEYS.lastRefresh);
    if (last && Date.now() - last < REFRESH_COOLDOWN) {
      const remaining = Math.ceil((REFRESH_COOLDOWN - (Date.now() - last)) / 1000);
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
      const minisRes = await fetch(`https://api.guildwars2.com/v2/account/minis?access_token=${apiKey}`);
      const skinsRes = await fetch(`https://api.guildwars2.com/v2/account/skins?access_token=${apiKey}`);
      const minis = await minisRes.json();
      const skins = await skinsRes.json();

      localStorage.setItem(STORAGE_KEYS.minis, JSON.stringify(minis));
      localStorage.setItem(STORAGE_KEYS.skins, JSON.stringify(skins));
      localStorage.setItem(STORAGE_KEYS.lastRefresh, Date.now());

      this.setStatus("API data refreshed");
      this.renderItems();
    } catch (e) {
      this.setStatus("API error");
    }
  }
}

customElements.define("gw2-unlocks", Gw2Unlocks);