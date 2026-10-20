import { first, integer, pick, toSearchParams, type RawSearchParams } from "@/lib/query-params";
import { LISTING_STATUSES } from "@/lib/rescuer/filters";
import type { AccountStatus, ReportReason, ReportStatus } from "@/types/admin";
import type { Role } from "@/types/api";
import type { ListingStatus } from "@/types/pets";

export const ADMIN_PAGE_SIZE = 20;

export const USER_ROLES = ["Adopter", "Rescuer", "Admin"] as const satisfies readonly Role[];
export const ACCOUNT_STATUSES = ["Active", "Disabled"] as const satisfies readonly AccountStatus[];
export const REPORT_STATUSES = ["Open", "Resolved", "Dismissed"] as const satisfies readonly ReportStatus[];
export const REPORT_REASONS = [
  "Spam",
  "Inappropriate",
  "Misleading",
  "SuspectedScam",
  "AnimalWelfare",
  "Other",
] as const satisfies readonly ReportReason[];

function text(value: string | undefined): string | undefined {
  const trimmed = value?.trim().slice(0, 100);
  return trimmed ? trimmed : undefined;
}

function link(base: string, params: URLSearchParams): string {
  const query = params.toString();
  return query ? `${base}?${query}` : base;
}

function withPaging(params: URLSearchParams, page: number): string {
  params.set("page", String(page));
  params.set("pageSize", String(ADMIN_PAGE_SIZE));
  return params.toString();
}

/* ---------- users ---------- */

export interface UserFilters {
  q?: string;
  role?: Role;
  status?: AccountStatus;
  page: number;
}

export function parseUserFilters(raw: RawSearchParams): UserFilters {
  return {
    q: text(first(raw.q)),
    role: pick(first(raw.role), USER_ROLES),
    status: pick(first(raw.status), ACCOUNT_STATUSES),
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

export const usersHref = (f: UserFilters, o: Partial<UserFilters> = {}) =>
  link("/dashboard/admin/users", toSearchParams({ ...f, page: 1, ...o }));

export const usersApiQuery = (f: UserFilters) => withPaging(toSearchParams({ ...f, page: 1 }), f.page);

export const hasUserFilters = (f: UserFilters) => Boolean(f.q || f.role || f.status);

/* ---------- listings ---------- */

export interface AdminPetFilters {
  q?: string;
  status?: ListingStatus;
  suspended?: boolean;
  page: number;
}

export function parseAdminPetFilters(raw: RawSearchParams): AdminPetFilters {
  const suspended = first(raw.suspended);

  return {
    q: text(first(raw.q)),
    status: pick(first(raw.status), LISTING_STATUSES),
    suspended: suspended === "true" ? true : suspended === "false" ? false : undefined,
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

function petParams(f: AdminPetFilters): URLSearchParams {
  return toSearchParams({
    q: f.q,
    status: f.status,
    suspended: f.suspended === undefined ? undefined : String(f.suspended),
    page: f.page,
  });
}

export const adminPetsHref = (f: AdminPetFilters, o: Partial<AdminPetFilters> = {}) =>
  link("/dashboard/admin/pets", petParams({ ...f, page: 1, ...o }));

export const adminPetsApiQuery = (f: AdminPetFilters) => withPaging(petParams({ ...f, page: 1 }), f.page);

export const hasAdminPetFilters = (f: AdminPetFilters) =>
  Boolean(f.q || f.status || f.suspended !== undefined);

/* ---------- reports ---------- */

export interface ReportFilters {
  status?: ReportStatus;
  reason?: ReportReason;
  petId?: number;
  page: number;
}

export function parseReportFilters(raw: RawSearchParams): ReportFilters {
  return {
    status: pick(first(raw.status), REPORT_STATUSES),
    reason: pick(first(raw.reason), REPORT_REASONS),
    petId: integer(first(raw.petId), 1, 2_147_483_647),
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

export const reportsHref = (f: ReportFilters, o: Partial<ReportFilters> = {}) =>
  link("/dashboard/admin/reports", toSearchParams({ ...f, page: 1, ...o }));

export const reportsApiQuery = (f: ReportFilters) => withPaging(toSearchParams({ ...f, page: 1 }), f.page);

export const hasReportFilters = (f: ReportFilters) => Boolean(f.status || f.reason || f.petId);