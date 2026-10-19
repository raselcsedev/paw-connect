import "server-only";

export function problem(status: number, title: string, detail?: string): Response {
  return new Response(JSON.stringify({ status, title, detail }), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

export async function relayError(upstream: Response): Promise<Response> {
  const headers = new Headers({
    "Content-Type": upstream.headers.get("content-type") ?? "application/problem+json",
  });

  const retryAfter = upstream.headers.get("retry-after");
  if (retryAfter) {
    headers.set("Retry-After", retryAfter);
  }

  return new Response(await upstream.text(), { status: upstream.status, headers });
}