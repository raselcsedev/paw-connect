import { backendFetch, clientIpHeaders } from "@/lib/server/backend";
import { assertSameOrigin } from "@/lib/server/csrf";
import { problem } from "@/lib/server/problem";
import { renewSession } from "@/lib/server/refresh";
import { getAccessToken } from "@/lib/server/session";

// Only these backend areas are reachable through the proxy.
// auth/login, auth/register, auth/refresh and auth/logout are deliberately absent:
// they return tokens, so only the dedicated route handlers may call them.
const ALLOWED = [
  /^auth\/me$/,
  /^pets(\/|$)/,
  /^applications(\/|$)/,
  /^notifications(\/|$)/,
  /^dashboard\//,
  /^admin\//,
];

const FORWARD_REQUEST_HEADERS = ["content-type", "accept", "accept-language"];

const FORWARD_RESPONSE_HEADERS = [
  "content-type",
  "cache-control",
  "retry-after",
  "content-disposition",
  "x-content-type-options",
  "content-security-policy",
  "www-authenticate",
];

type RouteContext = { params: Promise<{ path: string[] }> };

function hasBody(request: Request): boolean {
  return request.method !== "GET" && request.method !== "HEAD";
}

function buildTarget(segments: string[], search: string): string | null {
  if (segments.some((s) => s === "" || s === "." || s === "..")) {
    return null;
  }

  const relative = segments.join("/");

  if (!ALLOWED.some((pattern) => pattern.test(relative))) {
    return null;
  }

  return `/api/${segments.map(encodeURIComponent).join("/")}${search}`;
}

function send(request: Request, target: string, accessToken?: string): Promise<Response> {
  const headers = new Headers(clientIpHeaders(request));

  for (const name of FORWARD_REQUEST_HEADERS) {
    const value = request.headers.get(name);
    if (value) {
      headers.set(name, value);
    }
  }

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const body = hasBody(request);

  return backendFetch(target, {
    method: request.method,
    headers,
    body: body ? request.body : undefined,
    ...(body ? { duplex: "half" as const } : {}),
  });
}

function toResponse(upstream: Response): Response {
  const headers = new Headers();

  for (const name of FORWARD_RESPONSE_HEADERS) {
    const value = upstream.headers.get(name);
    if (value) {
      headers.set(name, value);
    }
  }

  return new Response(upstream.body, { status: upstream.status, headers });
}

async function handler(request: Request, context: RouteContext): Promise<Response> {
  const blocked = assertSameOrigin(request);
  if (blocked) {
    return blocked;
  }

  const { path } = await context.params;
  const target = buildTarget(path, new URL(request.url).search);

  if (!target) {
    return problem(404, "Not found", "Unknown API route.");
  }

  try {
    let accessToken = await getAccessToken();
    let renewedNow = false;

    if (!accessToken) {
      const renewal = await renewSession();

      if (renewal.status === "unavailable") {
        return problem(503, "Service unavailable", "Please try again in a moment.");
      }

      if (renewal.status === "renewed") {
        accessToken = renewal.accessToken;
        renewedNow = true;
      }
    }

    let upstream = await send(request, target, accessToken);

    if (upstream.status === 401 && accessToken && !renewedNow && !hasBody(request)) {
      const renewal = await renewSession();

      if (renewal.status === "renewed") {
        upstream = await send(request, target, renewal.accessToken);
      }
    }

    return toResponse(upstream);
  } catch {
    return problem(502, "Service unavailable", "The server is temporarily unavailable.");
  }
}

export {
  handler as GET,
  handler as POST,
  handler as PUT,
  handler as PATCH,
  handler as DELETE,
};