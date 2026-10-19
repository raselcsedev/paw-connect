import type { RawSearchParams } from "@/lib/pets/search";
import type { ApplicationStatus, ApplicationType } from "@/types/applications";

export const APPLICATION_STATUSES = ["Pending", "UnderReview", "Approved", "Rejected", "Withdrawn"] as const;
export const APPLICATION_TYPES = ["Adoption", "Foster"] as const;
export const APPLICATIONS_PAGE_SIZE = 10;

export interface ApplicationFilters {
  status?: ApplicationStatus;
  type?: ApplicationType;
  page: number;
}

function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function pick<T extends string>(value: string | undefined, allowed: readonly T[]): T | undefined {
  return allowed.find((item) => item === value);
}

export function parseApplicationFilters(raw: RawSearchParams): ApplicationFilters {
  const page = first(raw.page);

  return {
    status: pick(first(raw.status), APPLICATION_STATUSES),
    type: pick(first(raw.type), APPLICATION_TYPES),
    page: page && /^\d+$/.test(page) && Number(page) >= 1 && Number(page) <= 100000 ? Number(page) : 1,
  };
}

function toParams(filters: ApplicationFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.status) params.set("status", filters.status);
  if (filters.type) params.set("type", filters.type);
  if (filters.page > 1) params.set("page", String(filters.page));

  return params;
}

export function applicationsQuery(filters: ApplicationFilters): string {
  return toParams(filters).toString();
}

export function applicationsApiQuery(filters: ApplicationFilters): string {
  const params = toParams(filters);
  params.set("page", String(filters.page));
  params.set("pageSize", String(APPLICATIONS_PAGE_SIZE));
  return params.toString();
}

export function applicationsHref(filters: ApplicationFilters, overrides: Partial<ApplicationFilters> = {}): string {
  const query = toParams({ ...filters, page: 1, ...overrides }).toString();
  return query ? `/dashboard/my-applications?${query}` : "/dashboard/my-applications";
}

export function hasApplicationFilters(filters: ApplicationFilters): boolean {
  return Boolean(filters.status || filters.type);
}