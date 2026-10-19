"use client";

import { useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { fetchCurrentUser, login, logout, register } from "@/services/auth";

export const meKey = ["me"] as const;

export function purgeUserData(queryClient: QueryClient): void {
  queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== meKey[0] });
}

export function useCurrentUser() {
  return useQuery({
    queryKey: meKey,
    queryFn: fetchCurrentUser,
    staleTime: 60_000,
    retry: false,
  });
}

export function useLogin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: login,
    onSuccess: (user) => {
      purgeUserData(queryClient);
      queryClient.setQueryData(meKey, user);
    },
  });
}

export function useRegister() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: register,
    onSuccess: (user) => {
      purgeUserData(queryClient);
      queryClient.setQueryData(meKey, user);
    },
  });
}

export function useLogout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: logout,
    onSettled: () => {
      queryClient.setQueryData(meKey, null);
      purgeUserData(queryClient);
    },
  });
}