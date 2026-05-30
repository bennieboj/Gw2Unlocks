const accountState = {
  minis: [],
  skins: [],
  novelties: [],
  achievements: []
};

const STORAGE_KEYS = {
  apiKey: "gw2_api_key",
  minis: "gw2_account_minis",
  skins: "gw2_account_skins",
  novelties: "gw2_account_novelties",
  achievements: "gw2_account_achievements",
  lastRefresh: "gw2_last_refresh"
};

loadAccountState();

const REFRESH_COOLDOWN = 5 * 60 * 1000;

const unlockMap = JSON.parse(
  document.getElementById("unlock-map").textContent
);

const modal = document.getElementById("modal");
const modalName = document.getElementById("modal-name");
const modalIcon = document.getElementById("modal-icon");
const modalRequirement = document.getElementById("modal-requirement");
const closeModal = document.getElementById("close-modal");
const wikiLink = document.getElementById("wiki-link");

const apiInput = document.getElementById("apikey");
const statusEl = document.getElementById("status");

apiInput.value = localStorage.getItem(STORAGE_KEYS.apiKey) || "";

function setStatus(text) {
  statusEl.textContent = text;
}

function loadAccountState() {
  accountState.minis = JSON.parse(localStorage.getItem(STORAGE_KEYS.minis) || "[]");
  accountState.skins = JSON.parse(localStorage.getItem(STORAGE_KEYS.skins) || "[]");
  accountState.novelties = JSON.parse(localStorage.getItem(STORAGE_KEYS.novelties) || "[]");
  accountState.achievements = JSON.parse(localStorage.getItem(STORAGE_KEYS.achievements) || "[]");
}

function getColor(percent) {
  const hue = (percent / 100) * 120;
  return `hsl(${hue}, 80%, 50%)`;
}

function isUnlocked(id, type) {
  if (type === "Miniature") return accountState.minis.includes(id);
  if (type === "Skin") return accountState.skins.includes(id);
  if (type === "Novelty") return accountState.novelties.includes(id);
  if (type === "Achievement") return accountState.achievements.includes(id);
  return false;
}

function updateUnlockStates() {

  document.querySelectorAll(".grid").forEach(grid => {

    const items = [...grid.querySelectorAll(".item")];

    let unlocked = 0;

    items.forEach(item => {

      const id = Number(item.dataset.id);
      const type = item.dataset.type;
      if (isUnlocked(id, type)) {
        item.classList.add("unlocked");
        unlocked++;
      }
      else {
        item.classList.remove("unlocked");
      }
    });

    const title = grid.previousElementSibling;

    const total = Number(title.dataset.total);

    const percent =
      total === 0
        ? 0
        : Math.round((unlocked / total) * 100);

    title.querySelector(".type-unlocked").textContent = unlocked;
    title.querySelector(".type-percent").textContent = `${percent}%`;
    title.style.color = getColor(percent);
  });
}

document.querySelectorAll(".item").forEach(item => {

  item.addEventListener("click", () => {

    modalName.textContent = item.dataset.name || "";

    modalIcon.src = item.dataset.icon || "";

    modalRequirement.textContent =
      item.dataset.requirement || "";

    wikiLink.href = item.dataset.wiki || "#";

    modal.classList.add("open");
  });
});

closeModal.addEventListener("click", () => {
  modal.classList.remove("open");
});

modal.addEventListener("click", (e) => {
  if (e.target === modal) {
    modal.classList.remove("open");
  }
});

apiInput.value = localStorage.getItem(STORAGE_KEYS.apiKey) || "";

const menuToggle = document.getElementById("menu-toggle");
const sidebar = document.getElementById("sidebar");

menuToggle.addEventListener("click", () => {
  sidebar.classList.toggle("open");
});

apiInput.addEventListener("input", async () => {
  const key = apiInput.value.trim();

  if (key.length !== 72) {
    return;
  }

  const currentSaved =
    localStorage.getItem(STORAGE_KEYS.apiKey);

  if (currentSaved === key) {
    return;
  }

  localStorage.setItem(STORAGE_KEYS.apiKey, key);

  setStatus("API key saved");

  await refreshApi();
});

async function refreshApi() {

  const last = localStorage.getItem(STORAGE_KEYS.lastRefresh);

  if (last && Date.now() - parseInt(last) < REFRESH_COOLDOWN) {
    const remaining = Math.ceil(
      (REFRESH_COOLDOWN - (Date.now() - parseInt(last))) / 1000
    );

    setStatus(`Please wait ${remaining}s before refreshing again`);

    return;
  }

  const apiKey =
    localStorage.getItem(STORAGE_KEYS.apiKey);

  if (!apiKey) {
    setStatus("No API key set");
    return;
  }

  setStatus("Refreshing API data...");

  try {

    const [
      minisRes,
      skinsRes,
      noveltiesRes,
      achievementsRes
    ] = await Promise.all([
      fetch(`https://api.guildwars2.com/v2/account/minis?access_token=${apiKey}`),
      fetch(`https://api.guildwars2.com/v2/account/skins?access_token=${apiKey}`),
      fetch(`https://api.guildwars2.com/v2/account/novelties?access_token=${apiKey}`),
      fetch(`https://api.guildwars2.com/v2/account/achievements?access_token=${apiKey}`)
    ]);

    const minis = await minisRes.json();
    const skins = await skinsRes.json();
    const novelties = await noveltiesRes.json();

    const achievementData =
      await achievementsRes.json();

    const achievements =
      achievementData
        .filter(x => x.done)
        .map(x => x.id);

    localStorage.setItem(STORAGE_KEYS.minis, JSON.stringify(minis));
    localStorage.setItem(STORAGE_KEYS.skins, JSON.stringify(skins));
    localStorage.setItem(STORAGE_KEYS.novelties, JSON.stringify(novelties));
    localStorage.setItem(STORAGE_KEYS.achievements, JSON.stringify(achievements));
    localStorage.setItem(STORAGE_KEYS.lastRefresh, Date.now().toString());

    setStatus("API data refreshed");

    updateUnlockStates();
    updateSidebar();
  }
  catch (e) {
    console.error(e);
    setStatus("API refresh failed");
  }
}

function updateSidebar() {
  function count(map) {
    let unlocked = 0;
    let total = 0;

    for (const type in map) {
      const ids = map[type];

      total += ids.length;
      for (const id of ids) {
        if (isUnlocked(id, type)) {
          unlocked++;
        }
      }
    }

    return { unlocked, total };
  }

  // All
  let allUnlocked = 0;
  let allTotal = 0;
  // Groups
  for (const [slug, map] of Object.entries(unlockMap.groups)) {
    const { unlocked, total } = count(map);

    const el = document.querySelector(`[data-group="${slug}"] .sidebar-unlocked`);
    if (el) el.textContent = unlocked;
    const totalEl = document.querySelector(`[data-group="${slug}"] .sidebar-total`);
    if (totalEl) totalEl.textContent = total;

    allUnlocked += unlocked;
    allTotal += total;
  }
  const elAll = document.querySelector(`[data-group="all"] .sidebar-unlocked`);
  if (elAll) elAll.textContent = allUnlocked;
  const totalElAll = document.querySelector(`[data-group="all"] .sidebar-total`);
  if (totalElAll) totalElAll.textContent = allTotal;

  // Categories
  for (const [slug, map] of Object.entries(unlockMap.categories)) {
    const { unlocked, total } = count(map);

    const el = document.querySelector(`[data-category="${slug}"] .sidebar-unlocked`);
    if (el) el.textContent = unlocked;
    const totalEl = document.querySelector(`[data-category="${slug}"] .sidebar-total`);
    if (totalEl) totalEl.textContent = total;
  }
}

updateUnlockStates();
updateSidebar();
const existingKey = localStorage.getItem(STORAGE_KEYS.apiKey);

if (existingKey && existingKey.length === 72) {
  refreshApi();
}