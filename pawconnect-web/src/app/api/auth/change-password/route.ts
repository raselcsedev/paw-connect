import { backendFetch, clientIpHeaders } from "@/lib/server/backend";
import { assertSameOrigin } from "@/lib/server/csrf";
import { problem, relayError } from "@/lib/server/problem";
import { renewSession } from "@/lib/server/refresh";
import { getAccessToken, setSession } from "@/lib/server/session";
import type { AuthResponse } from "@/types/api";

export async function POST(request: Request): Promise<Response> {
  const blocked = assertSameOrigin(request);
  if (blocked) {
    return blocked;
  }

  let body: string;
  try {
    body = JSON.stringify(await request.json());
  } catch {
    return problem(400, "Bad request", "The request body must be valid JSON.");
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

    if (!accessToken) {
      return problem(401, "Unauthorized", "Please sign in again.");
    }

    const send = (token: string) =>
      backendFetch("/api/account/change-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
          ...clientIpHeaders(request),
        },
        body,
      });

    let upstream = await send(accessToken);

    if (upstream.status === 401 && !renewedNow) {
      const renewal = await renewSession();

      if (renewal.status === "renewed") {
        upstream = await send(renewal.accessToken);
      }
    }

    if (!upstream.ok) {
      return relayError(upstream);
    }

    // The API revoked every old session and issued a new one. Store it, and return no tokens.
    const auth = (await upstream.json()) as AuthResponse;
    await setSession(auth);

    return Response.json({ user: auth.user });
  } catch {
    return problem(502, "Service unavailable", "The server is temporarily unavailable.");
  }
}