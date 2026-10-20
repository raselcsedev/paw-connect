export type Species = "Dog" | "Cat" | "Rabbit" | "Bird" | "Other";
export type PetSize = "Small" | "Medium" | "Large";
export type PetSex = "Unknown" | "Male" | "Female";
export type AvailabilityType = "Adoption" | "Foster" | "Both";
export type ListingStatus =
  | "Draft"
  | "Available"
  | "Adopted"
  | "Unavailable"
  | "Archived";
export type PetSort = "Newest" | "Oldest" | "AgeAsc" | "AgeDesc" | "NameAsc";

export interface PetSummaryDto {
  id: number;
  name: string;
  species: Species;
  breed: string | null;
  ageInMonths: number;
  size: PetSize;
  city: string;
  availabilityType: AvailabilityType;
  primaryPhotoId: number | null;
}

export interface PetDto {
  id: number;
  name: string;
  species: Species;
  breed: string | null;
  ageInMonths: number;
  size: PetSize;
  sex: PetSex;
  description: string | null;
  healthStatus: string | null;
  isVaccinated: boolean;
  isSpayedNeutered: boolean;
  specialCareRequirements: string | null;
  availabilityType: AvailabilityType;
  requirements: string | null;
  city: string;
  area: string | null;
  status: ListingStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
  isSuspended: boolean;
  moderationNote: string | null;
  rescuerName: string | null;
}

export interface PetPhotoDto {
  id: number;
  petId: number;
  url: string;
  isPrimary: boolean;
  contentType: string;
  sizeInBytes: number;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}
