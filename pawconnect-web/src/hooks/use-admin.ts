"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  adminPetsApiQuery,
  reportsApiQuery,
  usersApiQuery,
  type AdminPetFilters,
  type ReportFilters,
  type UserFilters,
} from "@/lib/admin/filters";
import {
  fetchAdminDashboard,
  fetchAdminPets,
  fetchAdminReport,
  fetchAdminReports,
  fetchAdminUsers,
  moderatePet,
  reviewReport,
  setUserStatus,
} from "@/services/admin";
import { submitReport } from "@/services/reports";

export function useAdminDashboard() {
  return useQuery({ queryKey: ["dashboard", "admin"], queryFn: fetchAdminDashboard });
}

export function useAdminUsers(filters: UserFilters) {
  return useQuery({
    queryKey: ["admin", "users", usersApiQuery(filters)],
    queryFn: () => fetchAdminUsers(filters),
    placeholderData: keepPreviousData,
  });
}

export function useAdminPets(filters: AdminPetFilters) {
  return useQuery({
    queryKey: ["admin", "pets", adminPetsApiQuery(filters)],
    queryFn: () => fetchAdminPets(filters),
    placeholderData: keepPreviousData,
  });
}

export function useAdminReports(filters: ReportFilters) {
  return useQuery({
    queryKey: ["admin", "reports", reportsApiQuery(filters)],
    queryFn: () => fetchAdminReports(filters),
    placeholderData: keepPreviousData,
  });
}

export function useAdminReport(id: number) {
  return useQuery({ queryKey: ["admin", "report", id], queryFn: () => fetchAdminReport(id) });
}

function useRefreshAdmin() {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["admin"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    ]);
}

export function useSetUserStatus() {
  const refresh = useRefreshAdmin();
  return useMutation({ mutationFn: setUserStatus, onSettled: () => refresh() });
}

export function useModeratePet() {
  const refresh = useRefreshAdmin();
  return useMutation({ mutationFn: moderatePet, onSettled: () => refresh() });
}

export function useReviewReport() {
  const queryClient = useQueryClient();
  const refresh = useRefreshAdmin();

  return useMutation({
    mutationFn: reviewReport,
    onSuccess: (updated, variables) => queryClient.setQueryData(["admin", "report", variables.id], updated),
    onSettled: () => refresh(),
  });
}

export function useSubmitReport() {
  return useMutation({ mutationFn: submitReport });
}