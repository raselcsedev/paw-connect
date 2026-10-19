import "server-only";
import { backendFetch, clientIpHeaders } from "@/lib/server/backend";
import { assertSameOrigin } from "@/lib/server/csrf";
import { problem, relayError } from "@/lib/server/problem";
import { setSession } from "@/lib/server/session";
import type { AuthResponse } from "@/types/api";

export async function handleAuth(
  request: Request,
  backendPath: string,
  successStatus: number,
): Promise<Response> {
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

  let upstream: Response;
  try {
    upstream = await backendFetch(backendPath, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...clientIpHeaders(request) },
      body,
    });
  } catch {
    return problem(502, "Service unavailable", "The server is temporarily unavailable.");
  }

  if (!upstream.ok) {
    return relayError(upstream);
  }

  const auth = (await upstream.json()) as AuthResponse;
  await setSession(auth);

  return Response.json({ user: auth.user }, { status: successStatus });
}