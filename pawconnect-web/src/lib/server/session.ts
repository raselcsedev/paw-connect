import "server-only";
import { cookies } from "next/headers";
import { config } from "@/lib/config";
import { SESSION_COOKIE } from "@/lib/constants";
import type { AuthResponse } from "@/types/api";

export const ACCESS_COOKIE = "pc_access";
export const REFRESH_COOKIE = "pc_refresh";

const REFRESH_PATH = "/api";

const baseOptions = {
  httpOnly: true,
  secure: config.isProduction,
  sameSite: "lax" as const,
};

// .NET writes 7 fractional digits; keep 3 so every parser agrees.
function parseUtc(value: string): Date {
  return new Date(value.replace(/(\.\d{3})\d+/, "$1"));
}

export async function setSession(auth: AuthResponse): Promise<void> {
  const store = await cookies();
  const refreshExpires = parseUtc(auth.refreshTokenExpiresAtUtc);

  store.set(ACCESS_COOKIE, auth.accessToken, {
    ...baseOptions,
    path: "/",
    expires: parseUtc(auth.accessTokenExpiresAtUtc),
  });

  store.set(REFRESH_COOKIE, auth.refreshToken, {
    ...baseOptions,
    path: REFRESH_PATH,
    expires: refreshExpires,
  });

  // A non-secret hint that "a session probably exists". Only the proxy reads it.
  store.set(SESSION_COOKIE, "1", { ...baseOptions, path: "/", expires: refreshExpires });
}

export async function clearSession(): Promise<void> {
  const store = await cookies();
  store.delete({ name: ACCESS_COOKIE, path: "/" });
  store.delete({ name: REFRESH_COOKIE, path: REFRESH_PATH });
  store.delete({ name: SESSION_COOKIE, path: "/" });
}

export async function getAccessToken(): Promise<string | undefined> {
  return (await cookies()).get(ACCESS_COOKIE)?.value;
}

export async function getRefreshToken(): Promise<string | undefined> {
  return (await cookies()).get(REFRESH_COOKIE)?.value;
}