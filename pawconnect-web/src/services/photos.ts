import { apiFetch } from "@/lib/api-client";
import type { PetPhotoDto } from "@/types/pets";

export function fetchPhotos(petId: number): Promise<PetPhotoDto[]> {
  return apiFetch<PetPhotoDto[]>(`/pets/${petId}/photos`);
}

export function uploadPhoto({ petId, file }: { petId: number; file: File }): Promise<PetPhotoDto> {
  const body = new FormData();
  body.append("file", file);
  return apiFetch<PetPhotoDto>(`/pets/${petId}/photos`, { method: "POST", body });
}

export function setPrimaryPhoto({ petId, photoId }: { petId: number; photoId: number }): Promise<void> {
  return apiFetch<void>(`/pets/${petId}/photos/${photoId}/primary`, { method: "PATCH" });
}

export function deletePhoto({ petId, photoId }: { petId: number; photoId: number }): Promise<void> {
  return apiFetch<void>(`/pets/${petId}/photos/${photoId}`, { method: "DELETE" });
}