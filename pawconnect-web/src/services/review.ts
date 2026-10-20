import { apiFetch } from "@/lib/api-client";
import { incomingApiQuery, type IncomingFilters } from "@/lib/rescuer/application-filters";
import type { ApplicationDto, ApplicationStatus } from "@/types/applications";
import type { PagedResult, PetDto } from "@/types/pets";
import type { IncomingApplicationDto } from "@/types/rescuer";

export function fetchIncoming(filters: IncomingFilters): Promise<PagedResult<IncomingApplicationDto>> {
  return apiFetch<PagedResult<IncomingApplicationDto>>(`/applications/incoming?${incomingApiQuery(filters)}`);
}

export function fetchPetOptions(): Promise<PagedResult<PetDto>> {
  return apiFetch<PagedResult<PetDto>>("/pets/mine?page=1&pageSize=50");
}

export function changeApplicationStatus(input: {
  id: number;
  status: Exclude<ApplicationStatus, "Pending" | "Withdrawn">;
  version: number;
  note?: string;
}): Promise<ApplicationDto> {
  const { id, ...body } = input;
  return apiFetch<ApplicationDto>(`/applications/${id}/status`, { method: "PATCH", body });
}

export function completeAdoption({ id, version }: { id: number; version: number }): Promise<ApplicationDto> {
  return apiFetch<ApplicationDto>(`/applications/${id}/complete-adoption`, { method: "POST", body: { version } });
}