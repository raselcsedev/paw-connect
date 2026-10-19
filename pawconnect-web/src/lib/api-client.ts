import type { ProblemDetails } from "@/types/api";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  get fieldErrors(): Record<string, string[]> {
    return this.problem?.errors ?? {};
  }
}

type Options = {
  method?: string;
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
};

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

// One shared renewal, so parallel 401s don't each trigger a refresh.
let renewal: Promise<boolean> | null = null;

function renewSession(): Promise<boolean> {
  renewal ??= fetch("/api/bff/auth/me", { cache: "no-store" })
    .then((response) => response.ok)
    .catch(() => false)
    .finally(() => {
      renewal = null;
    });

  return renewal;
}

async function send(url: string, options: Options): Promise<Response> {
  const { body, headers, ...rest } = options;
  const isForm = typeof FormData !== "undefined" && body instanceof FormData;

  try {
    return await fetch(url, {
      ...rest,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        ...(body !== undefined && !isForm ? { "Content-Type": "application/json" } : {}),
        ...headers,
      },
      body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, null, "Cannot reach the server. Check your connection and try again.");
  }
}

function messageFor(status: number, problem: ProblemDetails | null): string {
  if (status === 429) return "Too many requests. Please wait a moment and try again.";
  if (problem?.detail) return problem.detail;

  const firstFieldError = problem?.errors ? Object.values(problem.errors)[0]?.[0] : undefined;
  if (firstFieldError) return firstFieldError;

  if (problem?.title) return problem.title;

  return status >= 500
    ? "Something went wrong on our side. Please try again."
    : "The request could not be completed.";
}

async function handle<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let problem: ProblemDetails | null = null;

    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = null;
    }

    throw new ApiError(response.status, problem, messageFor(response.status, problem));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** For the .NET API, through the BFF proxy (/api/bff/...). */
export async function apiFetch<T>(path: string, options: Options = {}): Promise<T> {
  const url = `/api/bff${path}`;
  let response = await send(url, options);

  if (response.status === 401 && path !== "/auth/me") {
    if (await renewSession()) {
      response = await send(url, options);
    }

    if (response.status === 401) {
      unauthorizedHandler?.();
    }
  }

  return handle<T>(response);
}

/** For login, register and logout (/api/auth/...). A 401 here means bad credentials. */
export async function authFetch<T>(path: string, options: Options = {}): Promise<T> {
  return handle<T>(await send(`/api/auth${path}`, options));
}