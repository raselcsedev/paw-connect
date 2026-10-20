"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deletePhoto, fetchPhotos, setPrimaryPhoto, uploadPhoto } from "@/services/photos";

export const photoKeys = {
  list: (petId: number) => ["pets", "photos", petId] as const,
};

export function usePetPhotos(petId: number) {
  return useQuery({
    queryKey: photoKeys.list(petId),
    queryFn: () => fetchPhotos(petId),
  });
}

/** Refreshes the photo list and the listings table (which shows each pet's cover photo). */
export function useRefreshPhotos(petId: number) {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: photoKeys.list(petId) }),
      queryClient.invalidateQueries({ queryKey: ["pets", "mine"] }),
    ]);
}

export function useUploadPhoto() {
  return useMutation({ mutationFn: uploadPhoto });
}

export function useSetPrimaryPhoto(petId: number) {
  const refresh = useRefreshPhotos(petId);
  return useMutation({ mutationFn: setPrimaryPhoto, onSettled: () => refresh() });
}

export function useDeletePhoto(petId: number) {
  const refresh = useRefreshPhotos(petId);
  return useMutation({ mutationFn: deletePhoto, onSettled: () => refresh() });
}