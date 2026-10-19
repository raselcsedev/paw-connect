import "server-only";

function required(name: string): string {
  const value = process.env[name];

  if (!value) {
    throw new Error(`Missing environment variable: ${name}. See .env.example.`);
  }

  return value.replace(/\/+$/, "");
}

export const config = {
  apiBaseUrl: required("API_BASE_URL"),
  appOrigin: required("APP_ORIGIN"),
  isProduction: process.env.NODE_ENV === "production",
  backendTimeoutMs: 30_000,
} as const;