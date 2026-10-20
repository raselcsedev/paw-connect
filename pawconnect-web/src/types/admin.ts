import type { ApplicationStatus, ApplicationType } from "@/types/applications";
import type { Role } from "@/types/api";
import type { ListingStatus, Species } from "@/types/pets";

export type AccountStatus = "Active" | "Disabled";
export type ReportReason = "Spam" | "Inappropriate" | "Misleading" | "SuspectedScam" | "AnimalWelfare" | "Other";
export type ReportStatus = "Open" | "Resolved" | "Dismissed";
export type ModerationAction = "None" | "Suspend" | "Restore" | "Archive";

export interface AdminUserDto {
  id: string;
  displayName: string;
  email: string;
  role: Role;
  status: AccountStatus;
  createdAtUtc: string;
}

export interface AdminPetDto {
  id: number;
  name: string;
  species: Species;
  status: ListingStatus;
  isSuspended: boolean;
  moderationNote: string | null;
  ownerName: string;
  openReports: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AdminReportSummaryDto {
  id: number;
  petId: number;
  petName: string;
  reason: ReportReason;
  status: ReportStatus;
  reporterName: string;
  createdAtUtc: string;
}

export interface AdminReportDto {
  id: number;
  petId: number;
  petName: string;
  petStatus: ListingStatus;
  petIsSuspended: boolean;
  ownerName: string;
  reporterName: string;
  reason: ReportReason;
  details: string | null;
  status: ReportStatus;
  createdAtUtc: string;
  reviewedBy: string | null;
  reviewedAtUtc: string | null;
  resolutionNote: string | null;
  otherOpenReportsForPet: number;
}

export interface AdminDashboardDto {
  generatedAtUtc: string;
  users: {
    total: number;
    byStatus: Record<AccountStatus, number>;
    byRole: Record<Role, number>;
    newLast7Days: number;
    newLast30Days: number;
  };
  pets: { total: number; byStatus: Record<ListingStatus, number> };
  applications: {
    total: number;
    byStatus: Record<ApplicationStatus, number>;
    byType: Record<ApplicationType, number>;
    completedAdoptions: number;
  };
  openReports: number;
}