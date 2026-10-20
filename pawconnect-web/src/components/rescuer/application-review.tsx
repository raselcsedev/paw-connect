"use client";

import Link from "next/link";
import { useRef, useState } from "react";
import { toast } from "sonner";
import { ApplicationAnswers } from "@/components/applications/application-answers";
import { HistoryTimeline } from "@/components/applications/history-timeline";
import { StatusBadge } from "@/components/applications/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button, buttonClasses } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { Textarea } from "@/components/ui/textarea";
import { useApplication } from "@/hooks/use-applications";
import { useChangeApplicationStatus, useCompleteAdoption } from "@/hooks/use-review";
import { ApiError } from "@/lib/api-client";
import { formatDateTime, TYPE_LABELS } from "@/lib/applications/format";
import { errorMessage } from "@/lib/errors";
import { reviewActions, reviewHelp, targetLabel, type ReviewTarget } from "@/lib/rescuer/review";
import type { ApplicationDto } from "@/types/applications";

const NOTE_MAX = 500;

type Confirm = { kind: "status"; target: ReviewTarget } | { kind: "complete" };

const successMessage: Record<ReviewTarget, string> = {
  UnderReview: "Marked as under review. The applicant was notified.",
  Approved: "Application approved. The applicant was notified.",
  Rejected: "Application rejected. The applicant was notified.",
};

function describeConfirm(confirm: Confirm | null, application: ApplicationDto) {
  const who = application.applicantName;
  const pet = application.petName;
  const kind = application.type === "Adoption" ? "adoption" : "foster";

  if (confirm?.kind === "complete") {
    return {
      title: `Complete the adoption of ${pet}?`,
      message: `${pet} will be marked as adopted by ${who}. The listing closes, every other open application for ${pet} is rejected automatically, and those applicants are notified. This cannot be undone.`,
      label: "Complete adoption",
      variant: "danger" as const,
    };
  }

  if (confirm?.target === "Approved") {
    return {
      title: `Approve ${who}'s ${kind} application?`,
      message: `${who} will be notified. Only one application can be approved for ${pet} at a time. ${
        application.type === "Adoption"
          ? "You can still reject it later, or complete the adoption when the handover is done."
          : "Approving a foster application does not mark the pet as adopted."
      }`,
      label: "Approve",
      variant: "primary" as const,
    };
  }

  return {
    title: application.status === "Approved" ? "Revoke this approval?" : `Reject ${who}'s application?`,
    message: `${who} will be notified and can read your note in the status history. A rejected application cannot be reopened.`,
    label: application.status === "Approved" ? "Revoke approval" : "Reject application",
    variant: "danger" as const,
  };
}

function DecisionPanel({ application }: { application: ApplicationDto }) {
  const change = useChangeApplicationStatus();
  const complete = useCompleteAdoption();

  const [note, setNote] = useState("");
  const [noteError, setNoteError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<Confirm | null>(null);
  const noteRef = useRef<HTMLTextAreaElement>(null);

  const { targets, canComplete } = reviewActions(application);
  const pending = change.isPending || complete.isPending;

  async function run(action: Confirm) {
    try {
      if (action.kind === "status") {
        await change.mutateAsync({
          id: application.id,
          status: action.target,
          version: application.version,
          note: note.trim() || undefined,
        });
        toast.success(successMessage[action.target]);
      } else {
        await complete.mutateAsync({ id: application.id, version: application.version });
        toast.success(`Adoption completed. ${application.petName} is now marked as adopted.`);
      }

      setNote("");
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setConfirm(null);
    }
  }

  function start(target: ReviewTarget) {
    if (target === "Rejected" && note.trim() === "") {
      setNoteError("Explain your decision. The applicant will see this note.");
      noteRef.current?.focus();
      return;
    }

    setNoteError(null);

    if (target === "UnderReview") {
      void run({ kind: "status", target });
      return;
    }

    setConfirm({ kind: "status", target });
  }

  const dialog = describeConfirm(confirm, application);

  return (
    <section aria-labelledby="decision-heading" className="rounded-2xl border border-stone-200 bg-white p-5">
      <h2 id="decision-heading" className="text-lg font-semibold text-stone-900">
        Your decision
      </h2>
      <p className="mt-1 text-sm text-stone-600">{reviewHelp(application)}</p>

      {(targets.length > 0 || canComplete) && (
        <div className="mt-4 space-y-4">
          {targets.length > 0 && (
            <div>
              <label htmlFor="decision-note" className="block text-sm font-medium text-stone-800">
                Note for the applicant {targets.includes("Rejected") && <span className="font-normal text-stone-500">(required to reject)</span>}
              </label>
              <Textarea
                id="decision-note"
                ref={noteRef}
                rows={3}
                maxLength={NOTE_MAX}
                value={note}
                invalid={!!noteError}
                aria-describedby={noteError ? "decision-note-error" : "decision-note-hint"}
                onChange={(event) => {
                  setNote(event.target.value);
                  if (noteError) setNoteError(null);
                }}
                className="mt-1.5"
              />
              <div className="mt-1 flex justify-between gap-3">
                <p id="decision-note-hint" className="text-xs text-stone-500">
                  The applicant can read this in the status history.
                </p>
                <p aria-hidden="true" className="text-xs text-stone-500">
                  {note.length}/{NOTE_MAX}
                </p>
              </div>
              {noteError && (
                <p id="decision-note-error" role="alert" className="mt-1 text-sm text-red-600">
                  {noteError}
                </p>
              )}
            </div>
          )}

          <div className="flex flex-wrap gap-2">
            {targets.map((target) => (
              <Button
                key={target}
                variant={target === "Rejected" ? "secondary" : target === "Approved" ? "primary" : "secondary"}
                disabled={pending}
                loading={change.isPending && change.variables?.status === target}
                onClick={() => start(target)}
              >
                {targetLabel(target, application.status)}
              </Button>
            ))}

            {canComplete && (
              <Button variant="danger" disabled={pending} onClick={() => setConfirm({ kind: "complete" })}>
                Complete adoption
              </Button>
            )}
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirm !== null}
        title={dialog.title}
        message={dialog.message}
        confirmLabel={dialog.label}
        confirmVariant={dialog.variant}
        cancelLabel="Go back"
        loading={pending}
        onConfirm={() => confirm && void run(confirm)}
        onCancel={() => setConfirm(null)}
      />
    </section>
  );
}

export function ApplicationReview({ id }: { id: number }) {
  const { data: application, isPending, isError, error, refetch } = useApplication(id);

  if (isPending) {
    return (
      <div role="status" aria-label="Loading application" className="animate-pulse space-y-4">
        <div className="h-8 w-1/2 rounded bg-stone-200" />
        <div className="h-20 rounded-2xl bg-stone-200" />
        <div className="h-72 rounded-2xl bg-stone-200" />
      </div>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 404) {
      return (
        <EmptyState
          title="We could not find that application"
          message="It may not exist, or it may be for someone else's pet."
          action={
            <Link href="/dashboard/rescuer/applications" className={buttonClasses("primary")}>
              Back to applications
            </Link>
          }
        />
      );
    }

    return <ErrorState message={error.message} onRetry={() => refetch()} />;
  }

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-stone-600">
        <Link href="/dashboard/rescuer/applications" className="hover:underline">
          ← Applications
        </Link>
      </nav>

      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-stone-900">Application from {application.applicantName}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm text-stone-700">for {application.petName}</span>
          <Badge>{TYPE_LABELS[application.type]}</Badge>
          <StatusBadge status={application.status} />
          <span className="text-sm text-stone-500">Submitted {formatDateTime(application.createdAtUtc)}</span>
        </div>
        <p className="flex flex-wrap gap-x-4 text-sm">
          <Link
            href={`/dashboard/rescuer/applications?petId=${application.petId}`}
            className="text-orange-700 hover:underline"
          >
            All applications for {application.petName}
          </Link>
          <Link href={`/dashboard/rescuer/pets/${application.petId}/edit`} className="text-orange-700 hover:underline">
            Open the listing
          </Link>
        </p>
      </div>

      {application.completedAtUtc && (
        <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
          Adoption completed on {formatDateTime(application.completedAtUtc)}.
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-5">
        <section className="rounded-2xl border border-stone-200 bg-white p-5 lg:col-span-3">
          <h2 className="text-lg font-semibold text-stone-900">Their answers</h2>
          <p className="mt-1 text-xs text-stone-500">
            These answers are visible only to you and the applicant. Please do not share them.
          </p>
          <div className="mt-4">
            <ApplicationAnswers application={application} />
          </div>
        </section>

        <div className="space-y-6 lg:col-span-2">
          <DecisionPanel key={application.version} application={application} />

          <section className="rounded-2xl border border-stone-200 bg-white p-5">
            <h2 className="text-lg font-semibold text-stone-900">Status history</h2>
            <p className="mt-1 text-xs text-stone-500">Latest first</p>
            <div className="mt-4">
              <HistoryTimeline history={application.history} />
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}