"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm, type DefaultValues } from "react-hook-form";
import { toast } from "sonner";
import { CharCount } from "@/components/forms/char-count";
import { FormField } from "@/components/forms/form-field";
import { FormSection } from "@/components/forms/form-section";
import { Button, buttonClasses } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useSavePet } from "@/hooks/use-rescuer";
import { applyServerErrors } from "@/lib/forms";
import { SIZES, SPECIES } from "@/lib/pets/constants";
import { SEX_LABELS, SEXES } from "@/lib/rescuer/format";
import { petSchema, toFormValues, toPetRequest, type PetFormValues } from "@/lib/validations/pet";
import type { PetDto } from "@/types/pets";

const FIELD_NAMES = [
  "name", "species", "breed", "sex", "ageYears", "ageMonths", "size", "description", "healthStatus",
  "isVaccinated", "isSpayedNeutered", "specialCareRequirements", "availabilityType", "requirements", "city", "area",
] as const;

const ALIASES = { ageInMonths: "ageYears" };

const emptyValues: DefaultValues<PetFormValues> = {
  name: "",
  breed: "",
  sex: "Unknown",
  ageYears: "",
  ageMonths: "",
  description: "",
  healthStatus: "",
  isVaccinated: false,
  isSpayedNeutered: false,
  specialCareRequirements: "",
  requirements: "",
  city: "",
  area: "",
};

const availabilityOptions = [
  { value: "Adoption", title: "Adoption", text: "A permanent home." },
  { value: "Foster", title: "Foster", text: "A temporary home." },
  { value: "Both", title: "Adoption or foster", text: "Open to either." },
] as const;

export function PetForm({ pet }: { pet?: PetDto }) {
  const router = useRouter();
  const save = useSavePet();

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<PetFormValues>({
    resolver: zodResolver(petSchema),
    defaultValues: pet ? toFormValues(pet) : emptyValues,
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const saved = await save.mutateAsync({ id: pet?.id, body: toPetRequest(values) });

      if (pet) {
        toast.success("Changes saved.");
      } else {
        toast.success("Listing saved as a draft. Review it and publish when you are ready.");
        router.push(`/dashboard/rescuer/pets/${saved.id}/edit`);
      }
    } catch (error) {
      const message = applyServerErrors(error, setError, FIELD_NAMES, ALIASES);
      if (message) {
        setError("root.serverError", { type: "server", message });
      }
    }
  });

  const ageError = errors.ageYears?.message ?? errors.ageMonths?.message;

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-6">
      {errors.root?.serverError && (
        <p role="alert" className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">
          {errors.root.serverError.message}
        </p>
      )}

      <FormSection title="The basics">
        <FormField id="name" label="Name" error={errors.name?.message}>
          <Input id="name" invalid={!!errors.name} aria-describedby={errors.name ? "name-error" : undefined} {...register("name")} />
        </FormField>

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField id="species" label="Species" error={errors.species?.message}>
            <Select
              id="species"
              defaultValue=""
              aria-invalid={errors.species ? true : undefined}
              aria-describedby={errors.species ? "species-error" : undefined}
              {...register("species")}
            >
              <option value="" disabled>
                Choose one
              </option>
              {SPECIES.map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </Select>
          </FormField>

          <FormField id="breed" label="Breed (optional)" error={errors.breed?.message}>
            <Input id="breed" invalid={!!errors.breed} {...register("breed")} />
          </FormField>

          <FormField id="size" label="Size" error={errors.size?.message}>
            <Select
              id="size"
              defaultValue=""
              aria-invalid={errors.size ? true : undefined}
              aria-describedby={errors.size ? "size-error" : undefined}
              {...register("size")}
            >
              <option value="" disabled>
                Choose one
              </option>
              {SIZES.map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </Select>
          </FormField>

          <FormField id="sex" label="Sex" error={errors.sex?.message}>
            <Select id="sex" {...register("sex")}>
              {SEXES.map((value) => (
                <option key={value} value={value}>
                  {SEX_LABELS[value]}
                </option>
              ))}
            </Select>
          </FormField>
        </div>

        <fieldset>
          <legend className="mb-1.5 text-sm font-medium text-stone-800">Age (an estimate is fine)</legend>
          <div className="grid max-w-sm grid-cols-2 gap-4">
            <div>
              <label htmlFor="ageYears" className="mb-1 block text-xs text-stone-600">
                Years
              </label>
              <Input
                id="ageYears"
                inputMode="numeric"
                invalid={!!errors.ageYears}
                aria-describedby={ageError ? "age-error" : undefined}
                {...register("ageYears")}
              />
            </div>
            <div>
              <label htmlFor="ageMonths" className="mb-1 block text-xs text-stone-600">
                Months
              </label>
              <Input
                id="ageMonths"
                inputMode="numeric"
                invalid={!!errors.ageMonths}
                aria-describedby={ageError ? "age-error" : undefined}
                {...register("ageMonths")}
              />
            </div>
          </div>
          {ageError && (
            <p id="age-error" role="alert" className="mt-1.5 text-sm text-red-600">
              {ageError}
            </p>
          )}
        </fieldset>
      </FormSection>

      <FormSection title="About this pet">
        <FormField id="description" label="Description (optional)" error={errors.description?.message}>
          <Textarea
            id="description"
            rows={5}
            invalid={!!errors.description}
            placeholder="Personality, history, what kind of home would suit them."
            {...register("description")}
          />
          <CharCount control={control} name="description" max={2000} />
        </FormField>

        <FormField id="healthStatus" label="Health (optional)" error={errors.healthStatus?.message}>
          <Textarea id="healthStatus" rows={2} invalid={!!errors.healthStatus} {...register("healthStatus")} />
          <CharCount control={control} name="healthStatus" max={500} />
        </FormField>

        <div className="flex flex-wrap gap-6">
          <label className="flex items-center gap-2 text-sm text-stone-800">
            <input type="checkbox" className="h-4 w-4 accent-orange-600" {...register("isVaccinated")} />
            Vaccinated
          </label>
          <label className="flex items-center gap-2 text-sm text-stone-800">
            <input type="checkbox" className="h-4 w-4 accent-orange-600" {...register("isSpayedNeutered")} />
            Spayed or neutered
          </label>
        </div>

        <FormField id="specialCareRequirements" label="Special care (optional)" error={errors.specialCareRequirements?.message}>
          <Textarea
            id="specialCareRequirements"
            rows={2}
            invalid={!!errors.specialCareRequirements}
            {...register("specialCareRequirements")}
          />
          <CharCount control={control} name="specialCareRequirements" max={1000} />
        </FormField>
      </FormSection>

      <FormSection title="How is this pet offered?">
        <div className="grid gap-3 sm:grid-cols-3">
          {availabilityOptions.map((option) => (
            <label
              key={option.value}
              className="flex cursor-pointer gap-3 rounded-xl border border-stone-200 p-3 text-sm has-[:checked]:border-orange-600 has-[:checked]:bg-orange-50"
            >
              <input type="radio" value={option.value} className="mt-1 accent-orange-600" {...register("availabilityType")} />
              <span>
                <span className="block font-medium text-stone-900">{option.title}</span>
                <span className="text-stone-600">{option.text}</span>
              </span>
            </label>
          ))}
        </div>
        {errors.availabilityType && (
          <p role="alert" className="text-sm text-red-600">
            {errors.availabilityType.message}
          </p>
        )}

        <FormField
          id="requirements"
          label="What do you ask of the new home? (optional)"
          error={errors.requirements?.message}
          hint="Shown to applicants, for example a fenced garden or no small children."
        >
          <Textarea id="requirements" rows={3} invalid={!!errors.requirements} {...register("requirements")} />
          <CharCount control={control} name="requirements" max={1000} />
        </FormField>
      </FormSection>

      <FormSection title="Where is the pet?">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField id="city" label="City" error={errors.city?.message}>
            <Input id="city" invalid={!!errors.city} aria-describedby={errors.city ? "city-error" : undefined} {...register("city")} />
          </FormField>
          <FormField
            id="area"
            label="Area (optional)"
            error={errors.area?.message}
            hint="A neighbourhood is enough. Please do not enter a street address."
          >
            <Input id="area" invalid={!!errors.area} aria-describedby={errors.area ? "area-error" : "area-hint"} {...register("area")} />
          </FormField>
        </div>
      </FormSection>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" size="lg" loading={save.isPending}>
          {pet ? "Save changes" : "Create draft"}
        </Button>
        <Link href="/dashboard/rescuer/pets" className={buttonClasses("secondary", "lg")}>
          {pet ? "Back to listings" : "Cancel"}
        </Link>
      </div>
    </form>
  );
}