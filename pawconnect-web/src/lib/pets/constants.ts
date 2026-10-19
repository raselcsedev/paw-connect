import type { PetSort } from "@/types/pets";

export const SPECIES = ["Dog", "Cat", "Rabbit", "Bird", "Other"] as const;
export const SIZES = ["Small", "Medium", "Large"] as const;
export const AVAILABILITY_FILTERS = ["Adoption", "Foster"] as const;
export const SORTS = ["Newest", "Oldest", "AgeAsc", "AgeDesc", "NameAsc"] as const;

export const SORT_LABELS: Record<PetSort, string> = {
  Newest: "Newest listings",
  Oldest: "Oldest listings",
  AgeAsc: "Youngest pets",
  AgeDesc: "Oldest pets",
  NameAsc: "Name A to Z",
};

export const PAGE_SIZE = 12;