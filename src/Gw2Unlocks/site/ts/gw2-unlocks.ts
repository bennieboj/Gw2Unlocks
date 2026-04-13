import { DATASET_URL } from "./config.ts";
import { UnlockItem } from "./unlock-item.ts";

const STORAGE_KEYS = {
  apiKey: "gw2_api_key",
  minis: "gw2_account_minis",
  skins: "gw2_account_skins",
  lastRefresh: "gw2_last_refresh"
} as const;

type StorageKey = keyof typeof STORAGE_KEYS;

const REFRESH_COOLDOWN = 5 * 60 * 1000;

class Gw2Unlocks extends HTMLElement {
  unlockData: any = {};

  async connectedCallback() {
    console.log(`using mode ${__VITE_MODE__}, dataset ${DATASET_URL}`);
    document.title = "GW2 Unlocks" + (__VITE_MODE__ === "development" ? " [DEV]" : "");

    try {
      const res = await fetch(DATASET_URL);
      this.unlockData = await res.json();
    } catch (e) {
      console.error("Failed to load dataset", e);
      this.unlockData = {};
    }

    this.render();
    this.renderItems();
  }

  render() {
    const apiKey = localStorage.getItem(STORAGE_KEYS.apiKey) || "";

    this.innerHTML = `
      <style>
        .group-title {
          font-size: 1.5em;
          font-weight: bold;
          text-decoration: underline;
          margin-top: 20px;
        }

        .category-title {
          font-size: 1.2em;
          font-weight: 600;
          margin-top: 12px;
        }

        .type-title {
          font-size: 1em;
          font-weight: 500;
          margin-top: 8px;
        }

        .grid {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin: 6px 0 10px 0;
        }

        .item {
          width: 80px;
          text-align: center;
          opacity: 0.4;
        }

        .item.unlocked {
          opacity: 1;
        }

        .item img {
          width: 64px;
          height: 64px;
        }
      </style>

      <div class="controls">
        API Key:
        <input id="apikey" value="${apiKey}" size="60">
        <button id="savekey">Save</button>
        <button id="refresh">Refresh API Data</button>
        <span id="status"></span>
      </div>

      <div id="content"></div>
    `;

    this.querySelector<HTMLButtonElement>("#savekey")!.onclick = () => this.saveKey();
    this.querySelector<HTMLButtonElement>("#refresh")!.onclick = () => this.refreshApi();
  }

  saveKey() {
    const keyInput = this.querySelector<HTMLInputElement>("#apikey")!;
    const key = keyInput.value.trim();
    localStorage.setItem(STORAGE_KEYS.apiKey, key);
    this.setStatus("API key saved");
  }

  setStatus(msg: string) {
    const statusEl = this.querySelector<HTMLSpanElement>("#status")!;
    statusEl.innerText = msg;
  }

  getStoredUnlocks(type: "minis" | "skins"): number[] {
    const data = localStorage.getItem(STORAGE_KEYS[type]);
    return data ? JSON.parse(data) : [];
  }

  renderItems() {
    const minisUnlocked = this.getStoredUnlocks("minis");
    const skinsUnlocked = this.getStoredUnlocks("skins");

    const container = this.querySelector<HTMLDivElement>("#content")!;
    container.innerHTML = "";

    const createItem = (unlock: any) => {
      if (!unlock?.ApiData) return null;

      const type = unlock.Node?.Type;
      const id = unlock.ApiData.id;
      const name = unlock.ApiData.name;
      const icon = unlock.ApiData.icon;

      const unlocked =
        type === "Miniature"
          ? minisUnlocked.includes(id)
          : type === "Skin"
          ? skinsUnlocked.includes(id)
          : false;

      return {
        type,
        html: `
          <div class="item ${unlocked ? "unlocked" : ""}">
            <img src="${icon}" alt="${name}">
            <div>${name}</div>
          </div>
        `
      };
    };

    const groupByType = (unlocks: any[]) => {
      const result: Record<string, string> = {};

      unlocks.forEach(u => {
        const item = createItem(u);
        if (!item) return;

        if (!result[item.type]) result[item.type] = "";
        result[item.type] += item.html;
      });

      return result;
    };

    const renderTypeSection = (title: string, content: string) => {
      return `
        <div class="type-title">${title}</div>
        <div class="grid">${content}</div>
      `;
    };

    this.unlockData.UnlockGroups?.forEach((group: any) => {
      let groupHTML = `<div class="group-title">${group.Name}</div>`;

      group.UnlockCategories?.forEach((cat: any) => {
        const grouped = groupByType(cat.Unlocks || []);

        let categoryHTML = `<div class="category-title">${cat.Name}</div>`;

        if (grouped["Skin"]) {
          categoryHTML += renderTypeSection("Skins", grouped["Skin"]);
        }

        if (grouped["Miniature"]) {
          categoryHTML += renderTypeSection("Miniatures", grouped["Miniature"]);
        }

        if (grouped["Novelty"]) {
          categoryHTML += renderTypeSection("Novelties", grouped["Novelty"]);
        }

        if (grouped["Achievement"]) {
          categoryHTML += renderTypeSection("Achievements", grouped["Achievement"]);
        }

        groupHTML += categoryHTML;
      });

      container.innerHTML += groupHTML;
    });
  }

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