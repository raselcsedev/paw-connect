import { ApiError } from "@/lib/api-client";

export function errorMessage(error: unknown, fallback = "Something went wrong. Please try again."): string {
  return error instanceof ApiError ? error.message : fallback;
}