"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { listingsQuery, type ListingFilters } from "@/lib/rescuer/filters";
import {
  archivePet,
  changeAvailability,
  fetchMyPets,
  fetchPet,
  fetchRescuerDashboard,
  savePet,
} from "@/services/pets";

export const petKeys = {
  mine: (filters: ListingFilters) => ["pets", "mine", listingsQuery(filters)] as const,
  detail: (id: number) => ["pets", "detail", id] as const,
};

export function useMyPets(filters: ListingFilters) {
  return useQuery({
    queryKey: petKeys.mine(filters),
    queryFn: () => fetchMyPets(filters),
    placeholderData: keepPreviousData,
  });
}

export function usePet(id: number) {
  return useQuery({
    queryKey: petKeys.detail(id),
    queryFn: () => fetchPet(id),
  });
}

export function useRescuerDashboard() {
  return useQuery({
    queryKey: ["dashboard", "rescuer"],
    queryFn: fetchRescuerDashboard,
  });
}

function useRefreshListings() {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["pets", "mine"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    ]);
}

export function useSavePet() {
  const queryClient = useQueryClient();
  const refresh = useRefreshListings();

  return useMutation({
    mutationFn: savePet,
    onSuccess: (saved) => {
      queryClient.setQueryData(petKeys.detail(saved.id), saved);
      void refresh();
    },
  });
}

export function useChangeAvailability() {
  const queryClient = useQueryClient();
  const refresh = useRefreshListings();

  return useMutation({
    mutationFn: changeAvailability,
    onSettled: (_data, _error, variables) => {
      void queryClient.invalidateQueries({ queryKey: petKeys.detail(variables.id) });
      return refresh();
    },
  });
}

export function useArchivePet() {
  const queryClient = useQueryClient();
  const refresh = useRefreshListings();

  return useMutation({
    mutationFn: archivePet,
    onSettled: (_data, _error, id) => {
      void queryClient.invalidateQueries({ queryKey: petKeys.detail(id) });
      return refresh();
    },
  });
}