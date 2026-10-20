import type {
  ApplicationStatus,
  ApplicationType,
} from "@/types/applications";
import type { AvailabilityType, ListingStatus, PetSex, PetSize, Species } from "@/types/pets";

export interface PetRequest {
  name: string;
  species: Species;
  breed?: string;
  ageInMonths: number;
  size: PetSize;
  sex: PetSex;
  description?: string;
  healthStatus?: string;
  isVaccinated: boolean;
  isSpayedNeutered: boolean;
  specialCareRequirements?: string;
  availabilityType: AvailabilityType;
  requirements?: string;
  city: string;
  area?: string;
}

export type SettableStatus = Extract<ListingStatus, "Draft" | "Available" | "Unavailable">;

export interface IncomingApplicationDto {
  id: number;
  petId: number;
  petName: string;
  applicantName: string;
  type: ApplicationType;
  status: ApplicationStatus;
  fosterStartDate: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface RescuerDashboardDto {
  totalPets: number;
  petsByStatus: Record<ListingStatus, number>;
  totalApplications: number;
  applicationsByStatus: Record<ApplicationStatus, number>;
  awaitingReview: number;
  awaitingCompletion: number;
  completedAdoptions: number;
  unreadNotifications: number;
  recentOpen: IncomingApplicationDto[];
}