import type { ApplicationType } from "@/types/applications";
import type { AvailabilityType } from "@/types/pets";

export function allowedTypes(availability: AvailabilityType): ApplicationType[] {
  if (availability === "Adoption") return ["Adoption"];
  if (availability === "Foster") return ["Foster"];
  return ["Adoption", "Foster"];
}