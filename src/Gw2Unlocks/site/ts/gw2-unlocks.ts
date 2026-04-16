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
        html, body {
          height: auto;
          overflow-y: auto;
        }

        :host {
          display: block;
        }

        .layout {
          display: flex;
          min-height: 100vh;
        }

        .sidebar {
          width: 260px;
          border-right: 1px solid #ccc;
          padding: 10px;
        }

        .content {
          flex: 1;
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
          cursor: pointer;
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
        .modal-backdrop {
          position: fixed;
          inset: 0;
          background: rgba(0,0,0,0.65);
          display: none;
          align-items: center;
          justify-content: center;
          z-index: 1000;
        }

        .modal-backdrop.open {
          display: flex;
        }

        .modal {
          background: #222;
          color: white;
          padding: 20px;
          border-radius: 12px;
          min-width: 320px;
          text-align: center;
        }

        .modal img {
          width: 96px;
          height: 96px;
          margin: 12px 0;
        }

        .modal-actions a,
        .modal-actions button {
          padding: 8px 12px;
          border: none;
          border-radius: 8px;
          text-decoration: none;
          cursor: pointer;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          min-width: 110px;
          font: inherit;
        }

        .modal-actions a {
          background: #3b82f6;
          color: white;
        }

        .modal-actions a:hover {
          opacity: 0.9;
        }

        .modal-actions button {
          background: #555;
          color: white;
        }

        .modal-actions button:hover {
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

      <div class="modal-backdrop" id="modal">
        <div class="modal">
          <div id="modal-name"></div>
          <img id="modal-icon">
          <div class="modal-actions">
            <a id="wiki-link" target="_blank">Open Wiki</a>
            <button id="close-modal">Close</button>
          </div>
        </div>
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

    this.querySelector("#close-modal")!
      .addEventListener("click", () => this.closeModal());

    this.querySelector("#modal")!
      .addEventListener("click", (e) => {
        if (e.target === e.currentTarget) this.closeModal();
      });
  }

  getChatLink(id: number, type: string) {
    let bytes: number[] = [];

    if (type === "Miniature" || type === "Novelty") {
      bytes = [
        0x02,                 // item header
        0x01,                 // quantity = 1
        id & 0xff,
        (id >> 8) & 0xff,
        (id >> 16) & 0xff,
        0x00                  // flags
      ];
    }
    else if (type === "Skin") {
      bytes = [
        0x0A,
        id & 0xff,
        (id >> 8) & 0xff,
        (id >> 16) & 0xff,
        (id >> 24) & 0xff
      ];
    }
    else if (type === "Outfit") {
      bytes = [
        0x0B,
        id & 0xff,
        (id >> 8) & 0xff,
        (id >> 16) & 0xff,
        (id >> 24) & 0xff
      ];
    }

    const binary = String.fromCharCode(...bytes);

    return `[&${btoa(binary)}]`;
  }

  getWikiUrl(id: number, type: string, name: string) {
    if (type === "Achievement") {
      return `https://wiki.guildwars2.com/index.php?search=${encodeURIComponent(name)}`;
    }

    const chatLink = this.getChatLink(id, type);

    return `https://wiki.guildwars2.com/index.php?title=Special%3ASearch&search=${encodeURIComponent(chatLink)}`;
  }

  openModal(data: any, type: string) {
    (this.querySelector("#modal-name") as HTMLElement).textContent = data.name;
    (this.querySelector("#modal-icon") as HTMLImageElement).src = data.icon;

    (this.querySelector("#wiki-link") as HTMLAnchorElement).href =
      this.getWikiUrl(data.wikiId, type, data.name);

    this.querySelector("#modal")!.classList.add("open");
  }

  closeModal() {
    this.querySelector("#modal")!.classList.remove("open");
  }

  activateItems() {
    this.querySelectorAll(".item").forEach(el => {
      el.addEventListener("click", () => {
        const node = el as HTMLElement;

        this.openModal({
          wikiId: Number(node.dataset.wikiId),
          name: node.dataset.name,
          icon: node.dataset.icon
        }, node.dataset.type!);
      });
    });
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
    else if (apiType.includes("outfit")) {
      type = "Outfit";
    }

    if (!type) return null;

    const id = unlock.ApiData.id;
    const name = unlock.ApiData.name;
    const icon = unlock.ApiData.icon;

    let wikiId = id;

    if (type === "Miniature") {
      wikiId = unlock.ApiData.item_id;
    }
    else if (type === "Novelty" || type === "Outfit") {
      wikiId = unlock.ApiData.unlock_item_ids?.[0];
    }

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
        <div
          class="item ${unlocked ? "unlocked" : ""}"
          data-type="${type}"
          data-wiki-id="${wikiId || ""}"
          data-name="${name.replace(/"/g, "&quot;")}"
          data-icon="${icon}"
        >
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
      Achievement: "Achievements",
      Outfit: "Outfits"
    };

    Object.keys(map).forEach(type => {
      if (!grouped[type]) return;

      const { unlocked, items, percent } = this.getCompletion(grouped, type);
      const color = this.getColor(percent);

      html += `
        <div class="type-title" style="color:${color}">
          ${map[type]} (${unlocked} / ${items} · ${percent}%)
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
    this.activateItems();
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
    this.activateItems();
  }

  renderCategory(group: any, cat: any) {
    const content = this.querySelector("#content")!;
    const grouped = this.groupByType(cat.Unlocks || []);

    content.innerHTML = `
      <div class="title-group">${group.Name}</div>
      <div class="title-category">${cat.Name}</div>
      ${this.renderTypes(grouped)}
    `;
    this.activateItems();
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