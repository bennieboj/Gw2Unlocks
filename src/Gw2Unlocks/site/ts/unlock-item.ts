// API unlock item interface
export interface UnlockItem {
  unlock_type: "minis" | "skins";
  unlock_typeid: number;
  name: string;
  icon: string;
}
