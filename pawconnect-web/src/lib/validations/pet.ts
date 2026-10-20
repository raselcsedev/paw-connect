import { z } from "zod";
import { SIZES, SPECIES } from "@/lib/pets/constants";
import { SEXES } from "@/lib/rescuer/format";
import type { PetDto } from "@/types/pets";
import type { PetRequest } from "@/types/rescuer";

const optionalText = (max: number) => z.string().trim().max(max, `Please keep this under ${max} characters.`);
const digits = z.string().trim().regex(/^\d*$/, "Use whole numbers only.");

const asNumber = (value: string): number => (value === "" ? 0 : Number(value));

export const petSchema = z
  .object({
    name: z.string().trim().min(2, "Enter at least 2 characters.").max(100, "Please keep this under 100 characters."),
    species: z.enum(SPECIES, { error: "Choose a species." }),
    breed: optionalText(100),
    sex: z.enum(SEXES),
    ageYears: digits,
    ageMonths: digits,
    size: z.enum(SIZES, { error: "Choose a size." }),
    description: optionalText(2000),
    healthStatus: optionalText(500),
    isVaccinated: z.boolean(),
    isSpayedNeutered: z.boolean(),
    specialCareRequirements: optionalText(1000),
    availabilityType: z.enum(["Adoption", "Foster", "Both"], { error: "Choose how this pet is offered." }),
    requirements: optionalText(1000),
    city: z.string().trim().min(2, "Enter the city.").max(100, "Please keep this under 100 characters."),
    area: optionalText(100),
  })
  .refine((v) => v.ageYears !== "" || v.ageMonths !== "", {
    message: "Enter the pet's age. Use 0 for a newborn.",
    path: ["ageYears"],
  })
  .refine(
    (v) => {
      const months = asNumber(v.ageMonths);
      return Number.isNaN(months) || months <= 11;
    },
    { message: "Months must be between 0 and 11.", path: ["ageMonths"] },
  )
  .refine(
    (v) => {
      const total = asNumber(v.ageYears) * 12 + asNumber(v.ageMonths);
      return Number.isNaN(total) || total <= 360;
    },
    { message: "The age cannot be more than 30 years.", path: ["ageYears"] },
  );

export type PetFormValues = z.infer<typeof petSchema>;

export function toFormValues(pet: PetDto): PetFormValues {
  return {
    name: pet.name,
    species: pet.species === "Dog" || pet.species === "Cat" || pet.species === "Rabbit" || pet.species === "Bird" ? pet.species : "Other",
    breed: pet.breed ?? "",
    sex: pet.sex,
    ageYears: String(Math.floor(pet.ageInMonths / 12)),
    ageMonths: String(pet.ageInMonths % 12),
    size: pet.size,
    description: pet.description ?? "",
    healthStatus: pet.healthStatus ?? "",
    isVaccinated: pet.isVaccinated,
    isSpayedNeutered: pet.isSpayedNeutered,
    specialCareRequirements: pet.specialCareRequirements ?? "",
    availabilityType: pet.availabilityType,
    requirements: pet.requirements ?? "",
    city: pet.city,
    area: pet.area ?? "",
  };
}

const blankToUndefined = (value: string): string | undefined => (value.trim() === "" ? undefined : value.trim());

export function toPetRequest(values: PetFormValues): PetRequest {
  return {
    name: values.name.trim(),
    species: values.species,
    breed: blankToUndefined(values.breed),
    ageInMonths: asNumber(values.ageYears) * 12 + asNumber(values.ageMonths),
    size: values.size,
    sex: values.sex,
    description: blankToUndefined(values.description),
    healthStatus: blankToUndefined(values.healthStatus),
    isVaccinated: values.isVaccinated,
    isSpayedNeutered: values.isSpayedNeutered,
    specialCareRequirements: blankToUndefined(values.specialCareRequirements),
    availabilityType: values.availabilityType,
    requirements: blankToUndefined(values.requirements),
    city: values.city.trim(),
    area: blankToUndefined(values.area),
  };
}