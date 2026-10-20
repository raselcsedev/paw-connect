"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { applicationKeys } from "@/hooks/use-applications";
import { incomingQuery, type IncomingFilters } from "@/lib/rescuer/application-filters";
import { changeApplicationStatus, completeAdoption, fetchIncoming, fetchPetOptions } from "@/services/review";

export function useIncomingApplications(filters: IncomingFilters) {
  return useQuery({
    queryKey: ["applications", "incoming", incomingQuery(filters)] as const,
    queryFn: () => fetchIncoming(filters),
    placeholderData: keepPreviousData,
  });
}

// Lives under ["pets","mine"] so it refreshes together with the listings.
export function usePetOptions() {
  return useQuery({
    queryKey: ["pets", "mine", "options"] as const,
    queryFn: fetchPetOptions,
    staleTime: 60_000,
  });
}

function useRefreshAfterReview() {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["applications"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["pets"] }),
    ]);
}

export function useChangeApplicationStatus() {
  const queryClient = useQueryClient();
  const refresh = useRefreshAfterReview();

  return useMutation({
    mutationFn: changeApplicationStatus,
    onSuccess: (updated, variables) => queryClient.setQueryData(applicationKeys.detail(variables.id), updated),
    onSettled: () => refresh(),
  });
}

export function useCompleteAdoption() {
  const queryClient = useQueryClient();
  const refresh = useRefreshAfterReview();

  return useMutation({
    mutationFn: completeAdoption,
    onSuccess: (updated, variables) => queryClient.setQueryData(applicationKeys.detail(variables.id), updated),
    onSettled: () => refresh(),
  });
}