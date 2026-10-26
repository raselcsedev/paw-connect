import { apiFetch, authFetch } from "@/lib/api-client";
import type { UserDto } from "@/types/api";

export function updateProfile(input: { displayName: string }): Promise<UserDto> {
  return apiFetch<UserDto>("/account/profile", { method: "PATCH", body: input });
}

// Returns tokens on the server side, so it goes through its own route handler, never the generic proxy.
export async function changePassword(input: { currentPassword: string; newPassword: string }): Promise<UserDto> {
  const result = await authFetch<{ user: UserDto }>("/change-password", { method: "POST", body: input });
  return result.user;
}