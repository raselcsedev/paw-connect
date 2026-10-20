import { apiFetch } from "@/lib/api-client";
import {
  adminPetsApiQuery,
  reportsApiQuery,
  usersApiQuery,
  type AdminPetFilters,
  type ReportFilters,
  type UserFilters,
} from "@/lib/admin/filters";
import type {
  AccountStatus,
  AdminDashboardDto,
  AdminPetDto,
  AdminReportDto,
  AdminReportSummaryDto,
  AdminUserDto,
  ModerationAction,
  ReportStatus,
} from "@/types/admin";
import type { PagedResult } from "@/types/pets";

export const fetchAdminDashboard = () => apiFetch<AdminDashboardDto>("/dashboard/admin");

export const fetchAdminUsers = (filters: UserFilters) =>
  apiFetch<PagedResult<AdminUserDto>>(`/admin/users?${usersApiQuery(filters)}`);

export const setUserStatus = ({ id, status }: { id: string; status: AccountStatus }) =>
  apiFetch<void>(`/admin/users/${id}/status`, { method: "PATCH", body: { status } });

export const fetchAdminPets = (filters: AdminPetFilters) =>
  apiFetch<PagedResult<AdminPetDto>>(`/admin/pets?${adminPetsApiQuery(filters)}`);

export const moderatePet = ({ id, action, note }: { id: number; action: Exclude<ModerationAction, "None">; note: string }) =>
  apiFetch<AdminPetDto>(`/admin/pets/${id}/moderation`, { method: "PATCH", body: { action, note } });

export const fetchAdminReports = (filters: ReportFilters) =>
  apiFetch<PagedResult<AdminReportSummaryDto>>(`/admin/reports?${reportsApiQuery(filters)}`);

export const fetchAdminReport = (id: number) => apiFetch<AdminReportDto>(`/admin/reports/${id}`);

export const reviewReport = ({
  id,
  ...body
}: {
  id: number;
  decision: Exclude<ReportStatus, "Open">;
  action: ModerationAction;
  note: string;
}) => apiFetch<AdminReportDto>(`/admin/reports/${id}`, { method: "PATCH", body });