import type { PagedResult } from "@/types/pets";

export type { PagedResult };

export type ApplicationType = "Adoption" | "Foster";
export type ApplicationStatus = "Pending" | "UnderReview" | "Approved" | "Rejected" | "Withdrawn";
export type LivingArrangement = "House" | "Apartment" | "SharedHousing" | "Other";

export interface StatusHistoryDto {
  fromStatus: ApplicationStatus | null;
  toStatus: ApplicationStatus;
  changedBy: string;
  changedAtUtc: string;
  note: string | null;
}

export interface ApplicationSummaryDto {
  id: number;
  petId: number;
  petName: string;
  type: ApplicationType;
  status: ApplicationStatus;
  fosterStartDate: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ApplicationDto {
  id: number;
  petId: number;
  petName: string;
  applicantName: string;
  type: ApplicationType;
  status: ApplicationStatus;
  livingArrangement: LivingArrangement;
  householdInfo: string;
  otherPetsInfo: string | null;
  previousExperience: string | null;
  carePlan: string;
  reasonForApplying: string;
  fosterStartDate: string | null;
  fosterDurationWeeks: number | null;
  additionalNotes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  completedAtUtc: string | null;
  version: number;
  history: StatusHistoryDto[];
}

export interface CreateApplicationRequest {
  petId: number;
  type: ApplicationType;
  livingArrangement: LivingArrangement;
  householdInfo: string;
  otherPetsInfo?: string;
  previousExperience?: string;
  carePlan: string;
  reasonForApplying: string;
  fosterStartDate?: string;
  fosterDurationWeeks?: number;
  additionalNotes?: string;
}

export interface ApplicantDashboardDto {
  totalApplications: number;
  activeApplications: number;
  byStatus: Record<ApplicationStatus, number>;
  unreadNotifications: number;
  recent: ApplicationSummaryDto[];
}