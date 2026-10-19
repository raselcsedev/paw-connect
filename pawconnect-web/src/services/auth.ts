import { ApiError, apiFetch, authFetch } from "@/lib/api-client";
import type { LoginRequest, RegisterRequest, UserDto } from "@/types/api";

export async function fetchCurrentUser(): Promise<UserDto | null> {
  try {
    return await apiFetch<UserDto>("/auth/me");
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }
    throw error;
  }
}

export async function login(input: LoginRequest): Promise<UserDto> {
  const result = await authFetch<{ user: UserDto }>("/login", { method: "POST", body: input });
  return result.user;
}

export async function register(input: RegisterRequest): Promise<UserDto> {
  const result = await authFetch<{ user: UserDto }>("/register", { method: "POST", body: input });
  return result.user;
}

export async function logout(): Promise<void> {
  await authFetch<void>("/logout", { method: "POST" });
}