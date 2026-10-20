import type { ListingStatus, PetSex } from "@/types/pets";

export const LISTING_LABELS: Record<ListingStatus, string> = {
  Draft: "Draft",
  Available: "Available",
  Unavailable: "Paused",
  Adopted: "Adopted",
  Archived: "Archived",
};

export const LISTING_TONES: Record<ListingStatus, "neutral" | "orange" | "blue" | "green" | "red"> = {
  Draft: "neutral",
  Available: "green",
  Unavailable: "orange",
  Adopted: "blue",
  Archived: "neutral",
};

export const SEXES = ["Unknown", "Male", "Female"] as const satisfies readonly PetSex[];

export const SEX_LABELS: Record<PetSex, string> = {
  Unknown: "Not known",
  Male: "Male",
  Female: "Female",
};

export const LISTING_HELP: Record<ListingStatus, string> = {
  Draft: "Only you can see this listing. Publish it from your listings page when it is ready.",
  Available: "Visible to everyone and open for applications.",
  Unavailable: "Paused: hidden from browsing and closed to new applications.",
  Adopted: "This pet found a home.",
  Archived: "Removed from the site.",
};