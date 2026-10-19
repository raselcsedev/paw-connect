import "server-only";
import { backendFetch } from "@/lib/server/backend";
import { clearSession, getRefreshToken, setSession } from "@/lib/server/session";
import type { AuthResponse } from "@/types/api";

type RefreshResult =
  | { status: "ok"; auth: AuthResponse }
  | { status: "invalid" }
  | { status: "unavailable" };

export type Renewal =
  | { status: "renewed"; accessToken: string }
  | { status: "none" }
  | { status: "invalid" }
  | { status: "unavailable" };

const DEDUPE_WINDOW_MS = 10_000;

const recent = new Map<string, { result: Promise<RefreshResult>; expiresAt: number }>();

async function callBackend(refreshToken: string): Promise<RefreshResult> {
  try {
    const response = await backendFetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    });

    if (response.ok) {
      return { status: "ok", auth: (await response.json()) as AuthResponse };
    }

    if (response.status === 400 || response.status === 401 || response.status === 403) {
      return { status: "invalid" };
    }

    return { status: "unavailable" };
  } catch {
    return { status: "unavailable" };
  }
}

function refreshTokens(refreshToken: string): Promise<RefreshResult> {
  const now = Date.now();

  for (const [key, entry] of recent) {
    if (entry.expiresAt < now) {
      recent.delete(key);
    }
  }

  const existing = recent.get(refreshToken);
  if (existing) {
    return existing.result;
  }

  const result = callBackend(refreshToken).then((outcome) => {
    if (outcome.status !== "ok") {
      recent.delete(refreshToken);
    }
    return outcome;
  });

  recent.set(refreshToken, { result, expiresAt: now + DEDUPE_WINDOW_MS });
  return result;
}

export async function renewSession(): Promise<Renewal> {
  const refreshToken = await getRefreshToken();

  if (!refreshToken) {
    return { status: "none" };
  }

  const result = await refreshTokens(refreshToken);

  if (result.status === "ok") {
    await setSession(result.auth);
    return { status: "renewed", accessToken: result.auth.accessToken };
  }

  if (result.status === "invalid") {
    await clearSession();
    return { status: "invalid" };
  }

  return { status: "unavailable" };
}