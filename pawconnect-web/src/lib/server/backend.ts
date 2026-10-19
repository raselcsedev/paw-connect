import "server-only";
import { config } from "@/lib/config";

type BackendInit = RequestInit & { duplex?: "half" };

export function backendFetch(path: string, init: BackendInit = {}): Promise<Response> {
  return fetch(`${config.apiBaseUrl}${path}`, {
    cache: "no-store",
    signal: AbortSignal.timeout(config.backendTimeoutMs),
    ...init,
  });
}

export function clientIpHeaders(request: Request): Record<string, string> {
  const forwardedFor = request.headers.get("x-forwarded-for");
  return forwardedFor ? { "X-Forwarded-For": forwardedFor } : {};
}