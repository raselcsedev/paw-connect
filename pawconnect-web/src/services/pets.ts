import { apiFetch } from "@/lib/api-client";
import { listingsApiQuery, type ListingFilters } from "@/lib/rescuer/filters";
import type { PagedResult, PetDto } from "@/types/pets";
import type { PetRequest, RescuerDashboardDto, SettableStatus } from "@/types/rescuer";

export function fetchMyPets(filters: ListingFilters): Promise<PagedResult<PetDto>> {
  return apiFetch<PagedResult<PetDto>>(`/pets/mine?${listingsApiQuery(filters)}`);
}

export function fetchPet(id: number): Promise<PetDto> {
  return apiFetch<PetDto>(`/pets/${id}`);
}

export function savePet({ id, body }: { id?: number; body: PetRequest }): Promise<PetDto> {
  return id === undefined
    ? apiFetch<PetDto>("/pets", { method: "POST", body })
    : apiFetch<PetDto>(`/pets/${id}`, { method: "PUT", body });
}

export function changeAvailability({ id, status }: { id: number; status: SettableStatus }): Promise<PetDto> {
  return apiFetch<PetDto>(`/pets/${id}/availability`, { method: "PATCH", body: { status } });
}

export function archivePet(id: number): Promise<void> {
  return apiFetch<void>(`/pets/${id}`, { method: "DELETE" });
}

export function fetchRescuerDashboard(): Promise<RescuerDashboardDto> {
  return apiFetch<RescuerDashboardDto>("/dashboard/rescuer");
}