import type { ApplicationStatus, ApplicationType, LivingArrangement } from "@/types/applications";

export const LIVING_ARRANGEMENTS = ["House", "Apartment", "SharedHousing", "Other"] as const satisfies readonly LivingArrangement[];

export const LIVING_LABELS: Record<LivingArrangement, string> = {
  House: "House",
  Apartment: "Apartment",
  SharedHousing: "Shared housing",
  Other: "Other",
};

export const TYPE_LABELS: Record<ApplicationType, string> = {
  Adoption: "Adoption",
  Foster: "Foster",
};

export const STATUS_LABELS: Record<ApplicationStatus, string> = {
  Pending: "Pending",
  UnderReview: "Under review",
  Approved: "Approved",
  Rejected: "Rejected",
  Withdrawn: "Withdrawn",
};

export const STATUS_TONES: Record<ApplicationStatus, "neutral" | "orange" | "blue" | "green" | "red"> = {
  Pending: "orange",
  UnderReview: "blue",
  Approved: "green",
  Rejected: "red",
  Withdrawn: "neutral",
};

export const STATUS_HELP: Record<ApplicationStatus, string> = {
  Pending: "The rescuer has not reviewed your application yet.",
  UnderReview: "The rescuer is reviewing your application.",
  Approved: "Good news: the rescuer approved your application.",
  Rejected: "The rescuer did not choose this application. You can still apply to other pets.",
  Withdrawn: "You withdrew this application.",
};

function parseUtc(iso: string): Date {
  // .NET writes 7 fractional digits; keep 3 so every parser agrees.
  return new Date(iso.replace(/(\.\d{3})\d+/, "$1"));
}

export function formatDateTime(iso: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(parseUtc(iso));
}

/** For plain dates from the API ("2026-12-01"). */
export function formatDateOnly(value: string): string {
  return new Intl.DateTimeFormat("en", { dateStyle: "medium", timeZone: "UTC" }).format(new Date(`${value}T00:00:00Z`));
}