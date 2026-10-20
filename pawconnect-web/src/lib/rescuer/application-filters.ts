import { APPLICATION_STATUSES, APPLICATION_TYPES } from "@/lib/applications/filters";
import { first, integer, pick, type RawSearchParams } from "@/lib/query-params";
import type { ApplicationStatus, ApplicationType } from "@/types/applications";

export const INCOMING_PAGE_SIZE = 10;

export interface IncomingFilters {
  status?: ApplicationStatus;
  type?: ApplicationType;
  petId?: number;
  page: number;
}

export function parseIncomingFilters(raw: RawSearchParams): IncomingFilters {
  return {
    status: pick(first(raw.status), APPLICATION_STATUSES),
    type: pick(first(raw.type), APPLICATION_TYPES),
    petId: integer(first(raw.petId), 1, 2_147_483_647),
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

function toParams(filters: IncomingFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.status) params.set("status", filters.status);
  if (filters.type) params.set("type", filters.type);
  if (filters.petId) params.set("petId", String(filters.petId));
  if (filters.page > 1) params.set("page", String(filters.page));

  return params;
}

export function incomingQuery(filters: IncomingFilters): string {
  return toParams(filters).toString();
}

export function incomingApiQuery(filters: IncomingFilters): string {
  const params = toParams(filters);
  params.set("page", String(filters.page));
  params.set("pageSize", String(INCOMING_PAGE_SIZE));
  return params.toString();
}

export function incomingHref(filters: IncomingFilters, overrides: Partial<IncomingFilters> = {}): string {
  const query = toParams({ ...filters, page: 1, ...overrides }).toString();
  return query ? `/dashboard/rescuer/applications?${query}` : "/dashboard/rescuer/applications";
}

export function hasIncomingFilters(filters: IncomingFilters): boolean {
  return Boolean(filters.status || filters.type || filters.petId);
}