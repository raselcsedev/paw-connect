import "server-only";
import { notFound } from "next/navigation";
import { cache } from "react";
import { PAGE_SIZE } from "@/lib/pets/constants";
import { parseFilters, toApiQuery, type PetFilters } from "@/lib/pets/search";
import { PublicApiError, publicGet } from "@/lib/server/public-api";
import type { PagedResult, PetDto, PetPhotoDto, PetSummaryDto } from "@/types/pets";

export function searchPets(filters: PetFilters, pageSize = PAGE_SIZE): Promise<PagedResult<PetSummaryDto>> {
  return publicGet<PagedResult<PetSummaryDto>>(`/api/pets?${toApiQuery(filters, pageSize)}`);
}

// cache() makes generateMetadata and the page share one API call per render.
export const getPet = cache(async (id: number): Promise<PetDto> => {
  try {
    return await publicGet<PetDto>(`/api/pets/${id}`);
  } catch (error) {
    if (error instanceof PublicApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }
});

export async function getPhotos(id: number): Promise<PetPhotoDto[]> {
  try {
    return await publicGet<PetPhotoDto[]>(`/api/pets/${id}/photos`);
  } catch {
    return [];
  }
}

export async function getLatestPets(count = 4): Promise<PetSummaryDto[]> {
  try {
    return (await searchPets(parseFilters({}), count)).items;
  } catch {
    return [];
  }
}