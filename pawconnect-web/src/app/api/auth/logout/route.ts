import { backendFetch, clientIpHeaders } from "@/lib/server/backend";
import { assertSameOrigin } from "@/lib/server/csrf";
import { clearSession, getRefreshToken } from "@/lib/server/session";

export async function POST(request: Request): Promise<Response> {
  const blocked = assertSameOrigin(request);
  if (blocked) {
    return blocked;
  }

  const refreshToken = await getRefreshToken();

  if (refreshToken) {
    try {
      await backendFetch("/api/auth/logout", {
        method: "POST",
        headers: { "Content-Type": "application/json", ...clientIpHeaders(request) },
        body: JSON.stringify({ refreshToken }),
      });
    } catch {
      // The API is unreachable. We still clear the browser's cookies below.
    }
  }

  await clearSession();
  return new Response(null, { status: 204 });
}