"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { meKey } from "@/hooks/use-auth";
import { changePassword, updateProfile } from "@/services/account";

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: updateProfile,
    onSuccess: (user) => {
      queryClient.setQueryData(meKey, user);
      // Status histories show display names, so refresh anything that lists them.
      void queryClient.invalidateQueries({ queryKey: ["applications"] });
    },
  });
}

export function useChangePassword() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: changePassword,
    onSuccess: (user) => queryClient.setQueryData(meKey, user),
  });
}