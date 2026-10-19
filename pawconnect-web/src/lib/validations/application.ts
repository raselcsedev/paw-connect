import { z } from "zod";
import { LIVING_ARRANGEMENTS } from "@/lib/applications/format";
import type { ApplicationType, CreateApplicationRequest } from "@/types/applications";

/** Today's date in UTC, formatted like an <input type="date"> value. The API checks dates in UTC too. */
export function todayUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

const optionalText = (max: number) => z.string().trim().max(max, `Please keep this under ${max} characters.`);

export const applicationSchema = z
  .object({
    type: z.enum(["Adoption", "Foster"]),
    livingArrangement: z.enum(LIVING_ARRANGEMENTS, { error: "Choose your living arrangement." }),
    householdInfo: z
      .string()
      .trim()
      .min(5, "Tell us a little about who lives in your home.")
      .max(1000, "Please keep this under 1000 characters."),
    otherPetsInfo: optionalText(1000),
    previousExperience: optionalText(1000),
    carePlan: z
      .string()
      .trim()
      .min(10, "Please write at least 10 characters.")
      .max(1500, "Please keep this under 1500 characters."),
    reasonForApplying: z
      .string()
      .trim()
      .min(10, "Please write at least 10 characters.")
      .max(1500, "Please keep this under 1500 characters."),
    fosterStartDate: z.string(),
    fosterDurationWeeks: z.string().trim(),
    additionalNotes: optionalText(1000),
  })
  .refine((v) => v.type !== "Foster" || v.fosterStartDate !== "", {
    message: "Choose a start date.",
    path: ["fosterStartDate"],
  })
  .refine((v) => v.type !== "Foster" || v.fosterStartDate === "" || v.fosterStartDate >= todayUtc(), {
    message: "The start date cannot be in the past.",
    path: ["fosterStartDate"],
  })
  .refine(
    (v) =>
      v.type !== "Foster" ||
      (/^\d+$/.test(v.fosterDurationWeeks) && Number(v.fosterDurationWeeks) >= 1 && Number(v.fosterDurationWeeks) <= 52),
    { message: "Enter a whole number of weeks from 1 to 52.", path: ["fosterDurationWeeks"] },
  );

export type ApplicationFormValues = z.infer<typeof applicationSchema>;

const blankToUndefined = (value: string): string | undefined => (value.trim() === "" ? undefined : value.trim());

export function toApplicationRequest(petId: number, values: ApplicationFormValues): CreateApplicationRequest {
  const type: ApplicationType = values.type;

  return {
    petId,
    type,
    livingArrangement: values.livingArrangement,
    householdInfo: values.householdInfo.trim(),
    otherPetsInfo: blankToUndefined(values.otherPetsInfo),
    previousExperience: blankToUndefined(values.previousExperience),
    carePlan: values.carePlan.trim(),
    reasonForApplying: values.reasonForApplying.trim(),
    fosterStartDate: type === "Foster" ? values.fosterStartDate : undefined,
    fosterDurationWeeks: type === "Foster" ? Number(values.fosterDurationWeeks) : undefined,
    additionalNotes: blankToUndefined(values.additionalNotes),
  };
}