import "server-only";
import { backendFetch } from "@/lib/server/backend";

export class PublicApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "PublicApiError";
    this.status = status;
  }
}

/** An anonymous GET to the .NET API, called directly from a server component. */
export async function publicGet<T>(path: string): Promise<T> {
  let response: Response;

  try {
    response = await backendFetch(path, { headers: { Accept: "application/json" } });
  } catch {
    throw new PublicApiError(0, "The pet service is unavailable right now.");
  }

  if (!response.ok) {
    throw new PublicApiError(response.status, `The pet service returned ${response.status}.`);
  }

  return (await response.json()) as T;
}