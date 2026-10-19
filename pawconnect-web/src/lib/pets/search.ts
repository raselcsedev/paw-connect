import type { AvailabilityType, PetSize, PetSort, Species } from "@/types/pets";
import { AVAILABILITY_FILTERS, PAGE_SIZE, SIZES, SORTS, SPECIES } from "@/lib/pets/constants";

export type RawSearchParams = Record<string, string | string[] | undefined>;

export interface PetFilters {
  q?: string;
  species?: Species;
  size?: PetSize;
  minAgeMonths?: number;
  maxAgeMonths?: number;
  availabilityType?: Exclude<AvailabilityType, "Both">;
  city?: string;
  vaccinated?: boolean;
  sort: PetSort;
  page: number;
}

function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function pick<T extends string>(value: string | undefined, allowed: readonly T[]): T | undefined {
  return allowed.find((item) => item === value);
}

function integer(value: string | undefined, min: number, max: number): number | undefined {
  if (value === undefined || !/^\d+$/.test(value)) return undefined;
  const number = Number(value);
  return number >= min && number <= max ? number : undefined;
}

function text(value: string | undefined): string | undefined {
  const trimmed = value?.trim().slice(0, 100);
  return trimmed ? trimmed : undefined;
}

/** Turns untrusted URL parameters into a safe, typed filter object. Invalid values are ignored. */
export function parseFilters(raw: RawSearchParams): PetFilters {
  let min = integer(first(raw.minAgeMonths), 0, 360);
  let max = integer(first(raw.maxAgeMonths), 0, 360);

  if (min !== undefined && max !== undefined && min > max) {
    [min, max] = [max, min];
  }

  return {
    q: text(first(raw.q)),
    species: pick(first(raw.species), SPECIES),
    size: pick(first(raw.size), SIZES),
    minAgeMonths: min,
    maxAgeMonths: max,
    availabilityType: pick(first(raw.availabilityType), AVAILABILITY_FILTERS),
    city: text(first(raw.city)),
    vaccinated: first(raw.vaccinated) === "true" ? true : undefined,
    sort: pick(first(raw.sort), SORTS) ?? "Newest",
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

export function hasActiveFilters(filters: PetFilters): boolean {
  return Boolean(
    filters.q ||
      filters.species ||
      filters.size ||
      filters.minAgeMonths !== undefined ||
      filters.maxAgeMonths !== undefined ||
      filters.availabilityType ||
      filters.city ||
      filters.vaccinated,
  );
}

function toParams(filters: PetFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.q) params.set("q", filters.q);
  if (filters.species) params.set("species", filters.species);
  if (filters.size) params.set("size", filters.size);
  if (filters.minAgeMonths !== undefined) params.set("minAgeMonths", String(filters.minAgeMonths));
  if (filters.maxAgeMonths !== undefined) params.set("maxAgeMonths", String(filters.maxAgeMonths));
  if (filters.availabilityType) params.set("availabilityType", filters.availabilityType);
  if (filters.city) params.set("city", filters.city);
  if (filters.vaccinated) params.set("vaccinated", "true");
  if (filters.sort !== "Newest") params.set("sort", filters.sort);
  if (filters.page > 1) params.set("page", String(filters.page));

  return params;
}

/** A canonical, stable string for the filters. Used as the Suspense key. */
export function toQueryString(filters: PetFilters): string {
  return toParams(filters).toString();
}

/** A link to /pets with some filters changed. Changing any filter returns to page 1. */
export function buildHref(filters: PetFilters, overrides: Partial<PetFilters> = {}): string {
  const query = toQueryString({ ...filters, page: 1, ...overrides });
  return query ? `/pets?${query}` : "/pets";
}

/** The query string sent to the .NET API. */
export function toApiQuery(filters: PetFilters, pageSize = PAGE_SIZE): string {
  const params = toParams(filters);
  params.set("sort", filters.sort);
  params.set("page", String(filters.page));
  params.set("pageSize", String(pageSize));
  return params.toString();
}