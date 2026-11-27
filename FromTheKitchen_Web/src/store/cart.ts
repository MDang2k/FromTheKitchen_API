import { useSyncExternalStore } from "react";
import type { MenuItem } from "../utility/constants";

export interface CartLine {
  item: MenuItem;
  quantity: number;
}

export const MAX_QUANTITY = 20;
const KEY = "cart";
const listeners = new Set<() => void>();

function load(): CartLine[] {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? "[]");
  } catch {
    return [];
  }
}

let lines = load();

function set(next: CartLine[]) {
  lines = next;
  localStorage.setItem(KEY, JSON.stringify(next));
  listeners.forEach((l) => l());
}

// Keep tabs in sync.
window.addEventListener("storage", (e) => {
  if (e.key !== KEY) return;
  lines = load();
  listeners.forEach((l) => l());
});

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function useCart() {
  return useSyncExternalStore(subscribe, () => lines);
}

// ponytail: prices are a snapshot from when the item was added; checkout must use the server's price.
export function setQuantity(item: MenuItem, quantity: number) {
  quantity = Math.min(quantity, MAX_QUANTITY);
  if (quantity <= 0) return set(lines.filter((l) => l.item.id !== item.id));
  const exists = lines.some((l) => l.item.id === item.id);
  set(
    exists
      ? lines.map((l) => (l.item.id === item.id ? { ...l, quantity } : l))
      : [...lines, { item, quantity }],
  );
}

export function clearCart() {
  set([]);
}
