import { apiFetch } from "@/lib/api-client";
import { applicationsApiQuery, type ApplicationFilters } from "@/lib/applications/filters";
import type {
  ApplicantDashboardDto,
  ApplicationDto,
  ApplicationSummaryDto,
  CreateApplicationRequest,
  PagedResult,
} from "@/types/applications";

export function submitApplication(input: CreateApplicationRequest): Promise<ApplicationDto> {
  return apiFetch<ApplicationDto>("/applications", { method: "POST", body: input });
}

export function fetchMyApplications(filters: ApplicationFilters): Promise<PagedResult<ApplicationSummaryDto>> {
  return apiFetch<PagedResult<ApplicationSummaryDto>>(`/applications/my?${applicationsApiQuery(filters)}`);
}

export function fetchApplication(id: number): Promise<ApplicationDto> {
  return apiFetch<ApplicationDto>(`/applications/${id}`);
}

export function withdrawApplication(id: number): Promise<ApplicationDto> {
  return apiFetch<ApplicationDto>(`/applications/${id}/withdraw`, { method: "POST" });
}

export function fetchApplicantDashboard(): Promise<ApplicantDashboardDto> {
  return apiFetch<ApplicantDashboardDto>("/dashboard/applicant");
}