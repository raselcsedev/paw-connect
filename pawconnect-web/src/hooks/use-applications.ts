"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { applicationsQuery, type ApplicationFilters } from "@/lib/applications/filters";
import {
  fetchApplicantDashboard,
  fetchApplication,
  fetchMyApplications,
  submitApplication,
  withdrawApplication,
} from "@/services/applications";

export const applicationKeys = {
  all: ["applications"] as const,
  mine: (filters: ApplicationFilters) => ["applications", "mine", applicationsQuery(filters)] as const,
  detail: (id: number) => ["applications", "detail", id] as const,
};

export function useMyApplications(filters: ApplicationFilters) {
  return useQuery({
    queryKey: applicationKeys.mine(filters),
    queryFn: () => fetchMyApplications(filters),
    placeholderData: keepPreviousData,
  });
}

export function useApplication(id: number) {
  return useQuery({
    queryKey: applicationKeys.detail(id),
    queryFn: () => fetchApplication(id),
  });
}

export function useApplicantDashboard() {
  return useQuery({
    queryKey: ["dashboard", "applicant"],
    queryFn: fetchApplicantDashboard,
  });
}

function useRefreshApplications() {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: applicationKeys.all }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    ]);
}

export function useSubmitApplication() {
  const refresh = useRefreshApplications();

  return useMutation({
    mutationFn: submitApplication,
    onSuccess: () => refresh(),
  });
}

export function useWithdrawApplication() {
  const refresh = useRefreshApplications();

  return useMutation({
    mutationFn: withdrawApplication,
    onSettled: () => refresh(),
  });
}