"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm, useWatch, type Control } from "react-hook-form";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useSubmitApplication } from "@/hooks/use-applications";
import { ApiError } from "@/lib/api-client";
import { LIVING_ARRANGEMENTS, LIVING_LABELS, TYPE_LABELS } from "@/lib/applications/format";
import { applyServerErrors } from "@/lib/forms";
import {
  applicationSchema,
  todayUtc,
  toApplicationRequest,
  type ApplicationFormValues,
} from "@/lib/validations/application";
import type { ApplicationType } from "@/types/applications";

const FIELD_NAMES = [
  "type",
  "livingArrangement",
  "householdInfo",
  "otherPetsInfo",
  "previousExperience",
  "carePlan",
  "reasonForApplying",
  "fosterStartDate",
  "fosterDurationWeeks",
  "additionalNotes",
] as const;

type CountedField =
  | "householdInfo"
  | "otherPetsInfo"
  | "previousExperience"
  | "carePlan"
  | "reasonForApplying"
  | "additionalNotes";

function CharCount({ control, name, max }: { control: Control<ApplicationFormValues>; name: CountedField; max: number }) {
  const value = useWatch({ control, name }) ?? "";

  return (
    <p className="text-right text-xs text-stone-500" aria-hidden="true">
      {value.length}/{max}
    </p>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <fieldset className="space-y-4 rounded-2xl border border-stone-200 bg-white p-5">
      <legend className="px-1 text-base font-semibold text-stone-900">{title}</legend>
      {children}
    </fieldset>
  );
}

type Props = {
  petId: number;
  petName: string;
  allowedTypes: ApplicationType[];
  initialType: ApplicationType;
};

export function ApplicationForm({ petId, petName, allowedTypes, initialType }: Props) {
  const router = useRouter();
  const submit = useSubmitApplication();

  const {
    register,
    handleSubmit,
    control,
    setError,
    watch,
    formState: { errors },
  } = useForm<ApplicationFormValues>({
    resolver: zodResolver(applicationSchema),
    defaultValues: {
      type: initialType,
      householdInfo: "",
      otherPetsInfo: "",
      previousExperience: "",
      carePlan: "",
      reasonForApplying: "",
      fosterStartDate: "",
      fosterDurationWeeks: "",
      additionalNotes: "",
    },
  });

  const type = watch("type");

  const onSubmit = handleSubmit(async (values) => {
    try {
      const application = await submit.mutateAsync(toApplicationRequest(petId, values));
      toast.success(`Your application for ${petName} was sent.`);
      router.push(`/dashboard/my-applications/${application.id}`);
    } catch (error) {
      const message = applyServerErrors(error, setError, FIELD_NAMES);
      if (message) {
        setError("root.serverError", { type: "server", message });
      }
    }
  });

  const conflict = submit.error instanceof ApiError && submit.error.status === 409;

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-6">
      {errors.root?.serverError && (
        <div role="alert" className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">
          <p>{errors.root.serverError.message}</p>
          {conflict && (
            <p className="mt-1">
              <Link href="/dashboard/my-applications" className="font-medium underline">
                View my applications
              </Link>
            </p>
          )}
        </div>
      )}

      {allowedTypes.length > 1 ? (
        <fieldset className="rounded-2xl border border-stone-200 bg-white p-5">
          <legend className="px-1 text-base font-semibold text-stone-900">What would you like to do?</legend>
          <div className="mt-2 grid gap-3 sm:grid-cols-2">
            {allowedTypes.map((value) => (
              <label
                key={value}
                className="flex cursor-pointer gap-3 rounded-xl border border-stone-200 p-3 text-sm has-[:checked]:border-orange-600 has-[:checked]:bg-orange-50"
              >
                <input type="radio" value={value} className="mt-1 accent-orange-600" {...register("type")} />
                <span>
                  <span className="block font-medium text-stone-900">
                    {value === "Adoption" ? "Adopt " : "Foster "}
                    {petName}
                  </span>
                  <span className="text-stone-600">
                    {value === "Adoption" ? "Give this pet a permanent home." : "Provide a temporary home."}
                  </span>
                </span>
              </label>
            ))}
          </div>
        </fieldset>
      ) : (
        <p className="rounded-xl bg-orange-50 px-4 py-3 text-sm text-orange-900">
          You are applying for <strong>{TYPE_LABELS[initialType].toLowerCase()}</strong>. {petName} is only offered for{" "}
          {TYPE_LABELS[initialType].toLowerCase()}.
        </p>
      )}

      <Section title="About your home">
        <FormField id="livingArrangement" label="Where do you live?" error={errors.livingArrangement?.message}>
          <Select
            id="livingArrangement"
            defaultValue=""
            aria-invalid={errors.livingArrangement ? true : undefined}
            aria-describedby={errors.livingArrangement ? "livingArrangement-error" : undefined}
            {...register("livingArrangement")}
          >
            <option value="" disabled>
              Choose one
            </option>
            {LIVING_ARRANGEMENTS.map((value) => (
              <option key={value} value={value}>
                {LIVING_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="householdInfo" label="Who lives with you?" error={errors.householdInfo?.message}>
          <Textarea
            id="householdInfo"
            rows={3}
            invalid={!!errors.householdInfo}
            aria-describedby={errors.householdInfo ? "householdInfo-error" : undefined}
            placeholder="For example: two adults and one child aged 8."
            {...register("householdInfo")}
          />
          <CharCount control={control} name="householdInfo" max={1000} />
        </FormField>
      </Section>

      <Section title="Your experience">
        <FormField id="otherPetsInfo" label="Other pets in your home (optional)" error={errors.otherPetsInfo?.message}>
          <Textarea
            id="otherPetsInfo"
            rows={3}
            invalid={!!errors.otherPetsInfo}
            aria-describedby={errors.otherPetsInfo ? "otherPetsInfo-error" : undefined}
            {...register("otherPetsInfo")}
          />
          <CharCount control={control} name="otherPetsInfo" max={1000} />
        </FormField>

        <FormField id="previousExperience" label="Experience with pets (optional)" error={errors.previousExperience?.message}>
          <Textarea
            id="previousExperience"
            rows={3}
            invalid={!!errors.previousExperience}
            aria-describedby={errors.previousExperience ? "previousExperience-error" : undefined}
            {...register("previousExperience")}
          />
          <CharCount control={control} name="previousExperience" max={1000} />
        </FormField>
      </Section>

      <Section title="Your plan">
        <FormField id="carePlan" label={`How will you care for ${petName}?`} error={errors.carePlan?.message}>
          <Textarea
            id="carePlan"
            invalid={!!errors.carePlan}
            aria-describedby={errors.carePlan ? "carePlan-error" : undefined}
            placeholder="Daily routine, time at home, exercise, vet care."
            {...register("carePlan")}
          />
          <CharCount control={control} name="carePlan" max={1500} />
        </FormField>

        <FormField id="reasonForApplying" label="Why are you applying?" error={errors.reasonForApplying?.message}>
          <Textarea
            id="reasonForApplying"
            invalid={!!errors.reasonForApplying}
            aria-describedby={errors.reasonForApplying ? "reasonForApplying-error" : undefined}
            {...register("reasonForApplying")}
          />
          <CharCount control={control} name="reasonForApplying" max={1500} />
        </FormField>
      </Section>

      {type === "Foster" && (
        <Section title="Foster details">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField id="fosterStartDate" label="Start date" error={errors.fosterStartDate?.message}>
              <Input
                id="fosterStartDate"
                type="date"
                min={todayUtc()}
                invalid={!!errors.fosterStartDate}
                aria-describedby={errors.fosterStartDate ? "fosterStartDate-error" : undefined}
                {...register("fosterStartDate")}
              />
            </FormField>

            <FormField
              id="fosterDurationWeeks"
              label="How many weeks?"
              error={errors.fosterDurationWeeks?.message}
              hint="From 1 to 52 weeks."
            >
              <Input
                id="fosterDurationWeeks"
                type="number"
                inputMode="numeric"
                min={1}
                max={52}
                invalid={!!errors.fosterDurationWeeks}
                aria-describedby={errors.fosterDurationWeeks ? "fosterDurationWeeks-error" : "fosterDurationWeeks-hint"}
                {...register("fosterDurationWeeks")}
              />
            </FormField>
          </div>
        </Section>
      )}

      <Section title="Anything else?">
        <FormField
          id="additionalNotes"
          label="Additional notes (optional)"
          error={errors.additionalNotes?.message}
          hint="Share only what the rescuer needs to decide. Please do not include ID numbers, bank details or your exact address."
        >
          <Textarea
            id="additionalNotes"
            rows={3}
            invalid={!!errors.additionalNotes}
            aria-describedby={errors.additionalNotes ? "additionalNotes-error" : "additionalNotes-hint"}
            {...register("additionalNotes")}
          />
          <CharCount control={control} name="additionalNotes" max={1000} />
        </FormField>
      </Section>

      <div className="space-y-3">
        <Button type="submit" size="lg" className="w-full sm:w-auto" loading={submit.isPending}>
          {type === "Foster" ? "Send foster application" : "Send adoption application"}
        </Button>
        <p className="text-sm text-stone-600">
          Your answers are visible only to you and the rescuer of this pet. You can withdraw a pending application at any time.
        </p>
      </div>
    </form>
  );
}