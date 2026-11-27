import { jwtDecode } from "jwt-decode";
import { API_BASE_URL, type MenuItem } from "./constants";

const TOKEN_KEY = "token";

export const formatPrice = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format;
export const formatDate = new Intl.DateTimeFormat("en-US", { dateStyle: "medium", timeStyle: "short" }).format;
export const imageUrl = (item: MenuItem) => `${API_BASE_URL}/${item.image}`;

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
}

interface TokenPayload {
  id: string;
  fullname?: string;
  email: string;
  role?: string;
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
  exp: number;
}

// Returns the logged-in user, or null when there is no token or it has expired.
export function getUser(): AuthUser | null {
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token) return null;
  try {
    const p = jwtDecode<TokenPayload>(token);
    if (p.exp * 1000 < Date.now()) {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }
    const role = p.role ?? p["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? "";
    return { id: p.id, name: p.fullname || p.email, email: p.email, role };
  } catch {
    localStorage.removeItem(TOKEN_KEY);
    return null;
  }
}

export function saveToken(token: string) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY);
}

// Calls the API with the login token. Throws an Error carrying the API's error messages on failure.
// FormData bodies go as multipart (the browser sets the boundary); anything else goes as JSON.
export async function apiFetch<T>(path: string, method = "GET", body?: unknown): Promise<T> {
  const token = localStorage.getItem(TOKEN_KEY);
  const isForm = body instanceof FormData;
  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: {
        ...(!isForm && { "Content-Type": "application/json" }),
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
    });
  } catch {
    throw new Error("Can't reach the server. Check that the API is running.");
  }
  if (res.status === 401) {
    logout();
    throw new Error("Your session has expired. Please log in again.");
  }
  if (res.status === 403) {
    throw new Error("You don't have permission to do that.");
  }
  const data = await res.json().catch(() => null);
  if (!res.ok || data?.isSuccess === false) {
    const messages: string[] = data?.errorMessages?.length ? data.errorMessages : ["Something went wrong. Please try again."];
    throw new Error(messages.join("\n"));
  }
  return data?.result as T;
}
