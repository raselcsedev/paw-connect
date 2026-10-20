import { formatDateOnly, LIVING_LABELS } from "@/lib/applications/format";
import type { ApplicationDto } from "@/types/applications";

function Answer({ label, value }: { label: string; value: string | null }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">{label}</dt>
      <dd className="mt-1 whitespace-pre-line text-sm text-stone-900">
        {value ?? <span className="text-stone-500">Not provided</span>}
      </dd>
    </div>
  );
}

export function ApplicationAnswers({ application }: { application: ApplicationDto }) {
  return (
    <dl className="space-y-4">
      <Answer label="Where they live" value={LIVING_LABELS[application.livingArrangement]} />
      <Answer label="Who lives with them" value={application.householdInfo} />
      <Answer label="Other pets" value={application.otherPetsInfo} />
      <Answer label="Experience with pets" value={application.previousExperience} />
      <Answer label="Care plan" value={application.carePlan} />
      <Answer label="Why they applied" value={application.reasonForApplying} />
      {application.type === "Foster" && (
        <>
          <Answer
            label="Foster start date"
            value={application.fosterStartDate ? formatDateOnly(application.fosterStartDate) : null}
          />
          <Answer
            label="Foster duration"
            value={application.fosterDurationWeeks ? `${application.fosterDurationWeeks} weeks` : null}
          />
        </>
      )}
      <Answer label="Additional notes" value={application.additionalNotes} />
    </dl>
  );
}