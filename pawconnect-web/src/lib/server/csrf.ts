import "server-only";
import { config } from "@/lib/config";
import { problem } from "@/lib/server/problem";

const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS"]);

export function assertSameOrigin(request: Request): Response | null {
  if (SAFE_METHODS.has(request.method)) {
    return null;
  }

  const origin = request.headers.get("origin");
  const fetchSite = request.headers.get("sec-fetch-site");

  const originOk = origin === config.appOrigin;
  const siteOk = fetchSite === null || fetchSite === "same-origin";

  if (!originOk || !siteOk) {
    return problem(403, "Forbidden", "Cross-site requests are not allowed.");
  }

  return null;
}