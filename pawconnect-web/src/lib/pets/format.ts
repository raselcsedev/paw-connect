import type { AvailabilityType, PetSex, Species } from "@/types/pets";

export const SPECIES_EMOJI: Record<Species, string> = {
  Dog: "🐶",
  Cat: "🐱",
  Rabbit: "🐰",
  Bird: "🐦",
  Other: "🐾",
};

export function formatAge(months: number): string {
  if (months < 1) return "Under 1 month";
  if (months < 24) return months === 1 ? "1 month" : `${months} months`;

  const years = Math.floor(months / 12);
  const rest = months % 12;
  const base = `${years} years`;

  return rest === 0 ? base : `${base}, ${rest} ${rest === 1 ? "month" : "months"}`;
}

export function availabilityLabel(type: AvailabilityType): string {
  return type === "Both" ? "Adoption or foster" : type === "Adoption" ? "Adoption" : "Foster";
}

export function sexLabel(sex: PetSex): string {
  return sex === "Unknown" ? "Not known" : sex;
}

export function photoSrc(petId: number, photoId: number): string {
  return `/api/bff/pets/${petId}/photos/${photoId}`;
}

export function formatDate(iso: string): string {
  // .NET writes 7 fractional digits; keep 3 so every parser agrees.
  const date = new Date(iso.replace(/(\.\d{3})\d+/, "$1"));
  return new Intl.DateTimeFormat("en", { dateStyle: "medium", timeZone: "UTC" }).format(date);
}