import { DATASET_URL } from "./config.ts";

const STORAGE_KEYS = {
  apiKey: "gw2_api_key",
  minis: "gw2_account_minis",
  skins: "gw2_account_skins",
  novelties: "gw2_account_novelties",
  lastRefresh: "gw2_last_refresh"
} as const;

const REFRESH_COOLDOWN = 5 * 60 * 1000;

class Gw2Unlocks extends HTMLElement {
  unlockData: any = {};

  async connectedCallback() {
    try {
      const res = await fetch(DATASET_URL);
      this.unlockData = await res.json();
    } catch {
      this.unlockData = {};
    }

    this.render();
    this.bindEvents();
    this.renderRoute();
  }

  render() {
    const apiKey = localStorage.getItem(STORAGE_KEYS.apiKey) || "";

    this.innerHTML = `
      <style>
        .layout {
          display: flex;
          height: 100vh;
        }

        .sidebar {
          width: 260px;
          overflow-y: auto;
          border-right: 1px solid #ccc;
          padding: 10px;
        }

        .content {
          flex: 1;
          overflow-y: auto;
          padding: 16px;
        }

        .group-link {
          font-weight: bold;
          margin-top: 10px;
          cursor: pointer;
        }

        .category-link {
          margin-left: 10px;
          cursor: pointer;
          font-size: 0.9em;
        }

        .title-group {
          font-size: 1.5em;
          font-weight: bold;
          margin-bottom: 10px;
        }

        .title-category {
          font-size: 1.2em;
          font-weight: 600;
          margin-top: 10px;
        }

        .type-title {
          margin-top: 8px;
          font-weight: 500;
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
        
        .controls {
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .kofi-btn {
          margin-left: 10px;
          padding: 6px 10px;
          background: #29abe0;
          color: white;
          border-radius: 6px;
          text-decoration: none;
          font-size: 0.9em;
          font-weight: 500;
          display: inline-flex;
          align-items: center;
          gap: 6px;
        }

        .kofi-btn:hover {
          opacity: 0.9;
        }
      </style>

      <div class="controls">
        API Key:
        <input id="apikey" value="${apiKey}" size="60">
        <button id="savekey">Save</button>
        <button id="refresh">Refresh API Data</button>
        <span id="status"></span>

        <a
          href="https://ko-fi.com/bennieboj"
          target="_blank"
          rel="noopener noreferrer"
          class="kofi-btn"
          style="margin-left:auto;"
        >
          ☕ Support Bennieboj on Ko-fi
        </a>
      </div>

      <div class="layout">
        <div class="sidebar" id="sidebar"></div>
        <div class="content" id="content"></div>
      </div>
    `;

    this.renderSidebar();
  }

  bindEvents() {
    this.querySelector("#savekey")!.addEventListener("click", () => {
      const key = (this.querySelector("#apikey") as HTMLInputElement).value;
      localStorage.setItem(STORAGE_KEYS.apiKey, key);
    });

    this.querySelector("#refresh")!.addEventListener("click", () => this.refreshApi());

    window.addEventListener("hashchange", () => this.renderRoute());
  }

  getCompletion(grouped: any, type: string) {
    const items = (grouped[type] || "").match(/class="item/g)?.length || 0;
    const unlocked = (grouped[type] || "").match(/class="item unlocked/g)?.length || 0;

    const percent = items === 0 ? 0 : Math.round((unlocked / items) * 100);

    return { unlocked, items, percent };
  }

  getColor(percent: number) {
    // 0 = red (0deg), 100 = green (120deg)
    const hue = (percent / 100) * 120;
    return `hsl(${hue}, 80%, 50%)`;
  }

  renderSidebar() {
    const sidebar = this.querySelector("#sidebar")!;
    sidebar.innerHTML = "";

    // ✅ ALL UNLOCKS link (top)
    const all = document.createElement("div");
    all.className = "group-link";
    all.textContent = "All Unlocks";
    all.onclick = () => {
      location.hash = "";
    };
    sidebar.appendChild(all);

    // Optional separator
    const hr = document.createElement("hr");
    sidebar.appendChild(hr);

    // Existing groups + categories
    this.unlockData.UnlockGroups?.forEach((group: any) => {
      const g = document.createElement("div");

      g.innerHTML = `<div class="group-link">${group.Name}</div>`;
      g.onclick = () => {
        location.hash = `group=${encodeURIComponent(group.Name)}`;
      };

      sidebar.appendChild(g);

      group.UnlockCategories?.forEach((cat: any) => {
        const c = document.createElement("div");
        c.className = "category-link";
        c.textContent = cat.Name;

        c.onclick = (e) => {
          e.stopPropagation();
          location.hash = `group=${encodeURIComponent(group.Name)}&category=${encodeURIComponent(cat.Name)}`;
        };

        sidebar.appendChild(c);
      });
    });
  }

  parseRoute() {
    const hash = location.hash.slice(1);
    const params = new URLSearchParams(hash);

    return {
      group: params.get("group"),
      category: params.get("category")
    };
  }

  renderRoute() {
    const { group, category } = this.parseRoute();

    if (!group) {
      this.renderAll();
      return;
    }

    const g = this.unlockData.UnlockGroups?.find((x: any) => x.Name === group);
    if (!g) return;

    if (!category) {
      this.renderGroup(g);
      return;
    }

    const c = g.UnlockCategories?.find((x: any) => x.Name === category);
    if (!c) return;

    this.renderCategory(g, c);
  }

  getStoredUnlocks(type: "minis" | "skins" | "novelties"): number[] {
    const data = localStorage.getItem(STORAGE_KEYS[type]);
    return data ? JSON.parse(data) : [];
  }

  createItem(unlock: any) {
    if (!unlock?.ApiData) return null;

    const minis = this.getStoredUnlocks("minis");
    const skins = this.getStoredUnlocks("skins");
    const novelties = this.getStoredUnlocks("novelties");

    const metadataType = unlock.Node?.Metadata?.type?.toLowerCase?.() || "";
    const apiType = unlock.ApiData?.type?.toLowerCase?.() || "";

    let type: string | null = null;

    if (metadataType.includes("miniature")) {
      type = "Miniature";
    }
    else if (unlock.Node?.Type === "Skin" || apiType.includes("skin")) {
      type = "Skin";
    }
    else if (metadataType.includes("novelty")) {
      type = "Novelty";
    }
    else if (unlock.Node?.Type === "Achievement") {
      type = "Achievement";
    }

    if (!type) return null;

    const id = unlock.ApiData.id;
    const name = unlock.ApiData.name;
    const icon = unlock.ApiData.icon;

    const unlocked =
      type === "Miniature"
        ? minis.includes(id)
        : type === "Skin"
        ? skins.includes(id)
        : type === "Novelty"
        ? novelties.includes(id)
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
  }

  groupByType(unlocks: any[]) {
    const res: any = {};
    unlocks.forEach(u => {
      const item = this.createItem(u);
      if (!item) return;
      if (!res[item.type]) res[item.type] = "";
      res[item.type] += item.html;
    });
    return res;
  }

  renderTypes(grouped: any) {
    let html = "";

    const map: any = {
      Skin: "Skins",
      Miniature: "Miniatures",
      Novelty: "Novelties",
      Achievement: "Achievements"
    };

    Object.keys(map).forEach(type => {
      if (!grouped[type]) return;

      const { percent } = this.getCompletion(grouped, type);
      const color = this.getColor(percent);

      html += `
        <div class="type-title" style="color:${color}">
          ${map[type]} (${percent}%)
        </div>
        <div class="grid">${grouped[type]}</div>
      `;
    });

    return html;
  }

  renderAll() {
    const content = this.querySelector("#content")!;

    let allUnlocks: any[] = [];

    // Collect everything from all groups
    this.unlockData.UnlockGroups?.forEach((group: any) => {
      // group-level unlocks
      if (group.Unlocks?.length) {
        allUnlocks = allUnlocks.concat(group.Unlocks);
      }

      // category-level unlocks
      group.UnlockCategories?.forEach((cat: any) => {
        if (cat.Unlocks?.length) {
          allUnlocks = allUnlocks.concat(cat.Unlocks);
        }
      });
    });

    const grouped = this.groupByType(allUnlocks);

    content.innerHTML = `
      <div class="title-group">All Unlocks</div>
      ${this.renderTypes(grouped)}
    `;
  }
  renderGroup(group: any) {
    const content = this.querySelector("#content")!;

    // Collect ALL unlocks from all categories
    let allUnlocks: any[] = [];

    group.UnlockCategories?.forEach((cat: any) => {
      if (cat.Unlocks?.length) {
        allUnlocks = allUnlocks.concat(cat.Unlocks);
      }
    });

    // Also include group-level unlocks if they exist
    if (group.Unlocks?.length) {
      allUnlocks = allUnlocks.concat(group.Unlocks);
    }

    const grouped = this.groupByType(allUnlocks);

    content.innerHTML = `
      <div class="title-group">${group.Name}</div>
      ${this.renderTypes(grouped)}
    `;
  }

  renderCategory(group: any, cat: any) {
    const content = this.querySelector("#content")!;
    const grouped = this.groupByType(cat.Unlocks || []);

    content.innerHTML = `
      <div class="title-group">${group.Name}</div>
      <div class="title-category">${cat.Name}</div>
      ${this.renderTypes(grouped)}
    `;
  }


  setStatus(msg: string) {
    const statusEl = this.querySelector<HTMLSpanElement>("#status")!;
    statusEl.innerText = msg;
  }

  async refreshApi() {
    const last = localStorage.getItem(STORAGE_KEYS.lastRefresh);

    if (last && Date.now() - parseInt(last) < REFRESH_COOLDOWN) {
      const remaining = Math.ceil(
        (REFRESH_COOLDOWN - (Date.now() - parseInt(last))) / 1000
      );
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
      const [minisRes, skinsRes, noveltiesRes] = await Promise.all([
        fetch(`https://api.guildwars2.com/v2/account/minis?access_token=${apiKey}`),
        fetch(`https://api.guildwars2.com/v2/account/skins?access_token=${apiKey}`),
        fetch(`https://api.guildwars2.com/v2/account/novelties?access_token=${apiKey}`)
      ]);

      const minis: number[] = await minisRes.json();
      const skins: number[] = await skinsRes.json();
      const novelties: number[] = await noveltiesRes.json();

      localStorage.setItem(STORAGE_KEYS.minis, JSON.stringify(minis));
      localStorage.setItem(STORAGE_KEYS.skins, JSON.stringify(skins));
      localStorage.setItem(STORAGE_KEYS.novelties, JSON.stringify(novelties));
      localStorage.setItem(STORAGE_KEYS.lastRefresh, Date.now().toString());

      this.setStatus("API data refreshed");

      // 🔥 Important change for routed UI
      this.renderRoute();
    } catch (e) {
      console.error("API error", e);
      this.setStatus("API error");
    }
  }
}

customElements.define("gw2-unlocks", Gw2Unlocks);