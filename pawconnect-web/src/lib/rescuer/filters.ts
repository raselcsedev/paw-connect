import { first, integer, pick, type RawSearchParams } from "@/lib/query-params";
import type { ListingStatus } from "@/types/pets";

export const LISTING_STATUSES = ["Draft", "Available", "Unavailable", "Adopted", "Archived"] as const satisfies readonly ListingStatus[];
export const LISTINGS_PAGE_SIZE = 10;

export interface ListingFilters {
  status?: ListingStatus;
  page: number;
}

export function parseListingFilters(raw: RawSearchParams): ListingFilters {
  return {
    status: pick(first(raw.status), LISTING_STATUSES),
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

function toParams(filters: ListingFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.status) params.set("status", filters.status);
  if (filters.page > 1) params.set("page", String(filters.page));

  return params;
}

export function listingsQuery(filters: ListingFilters): string {
  return toParams(filters).toString();
}

export function listingsApiQuery(filters: ListingFilters): string {
  const params = toParams(filters);
  params.set("page", String(filters.page));
  params.set("pageSize", String(LISTINGS_PAGE_SIZE));
  return params.toString();
}

export function listingsHref(filters: ListingFilters, overrides: Partial<ListingFilters> = {}): string {
  const query = toParams({ ...filters, page: 1, ...overrides }).toString();
  return query ? `/dashboard/rescuer/pets?${query}` : "/dashboard/rescuer/pets";
}