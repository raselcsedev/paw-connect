"use client";

import Link from "next/link";
import { HistoryTimeline } from "@/components/applications/history-timeline";
import { StatusBadge } from "@/components/applications/status-badge";
import { WithdrawButton } from "@/components/applications/withdraw-button";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useApplication } from "@/hooks/use-applications";
import { ApiError } from "@/lib/api-client";
import { formatDateOnly, formatDateTime, LIVING_LABELS, STATUS_HELP, TYPE_LABELS } from "@/lib/applications/format";

function Answer({ label, value }: { label: string; value: string | null }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">{label}</dt>
      <dd className="mt-1 whitespace-pre-line text-sm text-stone-900">{value ?? <span className="text-stone-500">Not provided</span>}</dd>
    </div>
  );
}

export function ApplicationDetail({ id }: { id: number }) {
  const { data, isPending, isError, error, refetch } = useApplication(id);

  if (isPending) {
    return (
      <div role="status" aria-label="Loading application" className="animate-pulse space-y-4">
        <div className="h-8 w-1/2 rounded bg-stone-200" />
        <div className="h-24 rounded-2xl bg-stone-200" />
        <div className="h-64 rounded-2xl bg-stone-200" />
      </div>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 404) {
      return (
        <EmptyState
          title="We could not find that application"
          message="It may not exist, or it may belong to someone else."
          action={
            <Link href="/dashboard/my-applications" className={buttonClasses("primary")}>
              Back to my applications
            </Link>
          }
        />
      );
    }

    return <ErrorState message={error.message} onRetry={() => refetch()} />;
  }

  const application = data;

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-stone-600">
        <Link href="/dashboard/my-applications" className="hover:underline">
          ← My applications
        </Link>
      </nav>

      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-stone-900">
            Application for{" "}
            <Link href={`/pets/${application.petId}`} className="underline-offset-2 hover:underline">
              {application.petName}
            </Link>
          </h1>
          <div className="flex flex-wrap items-center gap-2">
            <Badge>{TYPE_LABELS[application.type]}</Badge>
            <StatusBadge status={application.status} />
            <span className="text-sm text-stone-500">Submitted {formatDateTime(application.createdAtUtc)}</span>
          </div>
        </div>

        {application.status === "Pending" && <WithdrawButton id={application.id} petName={application.petName} size="md" />}
      </div>

      <div role="status" className="rounded-2xl border border-stone-200 bg-white p-4 text-sm text-stone-700">
        <p>{STATUS_HELP[application.status]}</p>
        {application.completedAtUtc && (
          <p className="mt-1 font-medium text-emerald-800">
            Adoption completed on {formatDateTime(application.completedAtUtc)}. Congratulations!
          </p>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-5">
        <section className="rounded-2xl border border-stone-200 bg-white p-5 lg:col-span-3">
          <h2 className="text-lg font-semibold text-stone-900">Your answers</h2>
          <dl className="mt-4 space-y-4">
            <Answer label="Where you live" value={LIVING_LABELS[application.livingArrangement]} />
            <Answer label="Who lives with you" value={application.householdInfo} />
            <Answer label="Other pets" value={application.otherPetsInfo} />
            <Answer label="Experience with pets" value={application.previousExperience} />
            <Answer label="Care plan" value={application.carePlan} />
            <Answer label="Why you applied" value={application.reasonForApplying} />
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
        </section>

        <section className="rounded-2xl border border-stone-200 bg-white p-5 lg:col-span-2">
          <h2 className="text-lg font-semibold text-stone-900">Status history</h2>
          <p className="mt-1 text-xs text-stone-500">Latest first</p>
          <div className="mt-4">
            <HistoryTimeline history={application.history} />
          </div>
        </section>
      </div>
    </div>
  );
}