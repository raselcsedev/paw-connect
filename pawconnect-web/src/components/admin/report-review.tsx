"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { ListingBadges } from "@/components/rescuer/listing-badges";
import { Badge } from "@/components/ui/badge";
import { Button, buttonClasses } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { Textarea } from "@/components/ui/textarea";
import { useAdminReport, useReviewReport } from "@/hooks/use-admin";
import { ApiError } from "@/lib/api-client";
import { REASON_LABELS, REPORT_STATUS_LABELS, REPORT_STATUS_TONES } from "@/lib/admin/format";
import { formatDateTime } from "@/lib/applications/format";
import { errorMessage } from "@/lib/errors";
import { reviewSchema, type ReviewValues } from "@/lib/validations/moderation";
import type { AdminReportDto } from "@/types/admin";

function consequences(report: AdminReportDto, values: ReviewValues): string {
  if (values.decision === "Dismissed") {
    return `The report is closed with no action. The reporter is told it was reviewed. ${report.petName} is not changed.`;
  }

  const others =
    report.otherOpenReportsForPet > 0
      ? ` The ${report.otherOpenReportsForPet} other open ${report.otherOpenReportsForPet === 1 ? "report" : "reports"} about this listing will be resolved too.`
      : "";

  if (values.action === "Suspend") {
    return `${report.petName} is hidden from the public and ${report.ownerName} cannot publish it until you restore it. ${report.ownerName} is notified and sees your note.${others}`;
  }

  if (values.action === "Archive") {
    return `${report.petName} is removed. Every open application for it is rejected and those applicants are notified. ${report.ownerName} sees your note. This cannot be undone.${others}`;
  }

  return `The report is closed as resolved without changing the listing. The reporter is told it was reviewed.${others}`;
}

function ReviewForm({ report }: { report: AdminReportDto }) {
  const review = useReviewReport();
  const [confirm, setConfirm] = useState<ReviewValues | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<ReviewValues>({
    resolver: zodResolver(reviewSchema),
    defaultValues: { decision: "Resolved", action: "None", note: "" },
  });

  const decision = watch("decision");
  const canSuspend = !report.petIsSuspended && report.petStatus !== "Adopted" && report.petStatus !== "Archived";
  const canArchive = report.petStatus !== "Archived";

  async function run(values: ReviewValues) {
    try {
      await review.mutateAsync({
        id: report.id,
        decision: values.decision,
        action: values.decision === "Dismissed" ? "None" : values.action,
        note: values.note,
      });
      toast.success(values.decision === "Dismissed" ? "Report dismissed." : "Report resolved.");
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setConfirm(null);
    }
  }

  const effective: ReviewValues | null = confirm
    ? { ...confirm, action: confirm.decision === "Dismissed" ? "None" : confirm.action }
    : null;

  return (
    <form onSubmit={handleSubmit((values) => setConfirm(values))} noValidate className="space-y-4">
      <fieldset>
        <legend className="mb-2 text-sm font-medium text-stone-800">Decision</legend>
        <div className="grid gap-3 sm:grid-cols-2">
          {[
            { value: "Resolved", title: "Resolve", text: "The report is valid." },
            { value: "Dismissed", title: "Dismiss", text: "No problem found." },
          ].map((option) => (
            <label
              key={option.value}
              className="flex cursor-pointer gap-3 rounded-xl border border-stone-200 p-3 text-sm has-[:checked]:border-orange-600 has-[:checked]:bg-orange-50"
            >
              <input type="radio" value={option.value} className="mt-1 accent-orange-600" {...register("decision")} />
              <span>
                <span className="block font-medium text-stone-900">{option.title}</span>
                <span className="text-stone-600">{option.text}</span>
              </span>
            </label>
          ))}
        </div>
      </fieldset>

      <FormField id="review-action" label="Action on the listing">
        <Select id="review-action" disabled={decision === "Dismissed"} {...register("action")}>
          <option value="None">No action</option>
          <option value="Suspend" disabled={!canSuspend}>
            Suspend the listing
          </option>
          <option value="Archive" disabled={!canArchive}>
            Archive (remove) the listing
          </option>
        </Select>
      </FormField>

      <FormField
        id="review-note"
        label="Note"
        error={errors.note?.message}
        hint="The listing owner reads this note when you take action. Reporters only see that the report was reviewed."
      >
        <Textarea
          id="review-note"
          rows={3}
          maxLength={300}
          invalid={!!errors.note}
          aria-describedby={errors.note ? "review-note-error" : "review-note-hint"}
          {...register("note")}
        />
      </FormField>

      <Button type="submit" disabled={review.isPending}>
        Continue
      </Button>

      <ConfirmDialog
        open={confirm !== null}
        title={effective?.decision === "Dismissed" ? "Dismiss this report?" : "Resolve this report?"}
        message={effective ? consequences(report, effective) : ""}
        confirmLabel={effective?.decision === "Dismissed" ? "Dismiss report" : "Resolve report"}
        confirmVariant={effective?.action && effective.action !== "None" ? "danger" : "primary"}
        cancelLabel="Go back"
        loading={review.isPending}
        onConfirm={() => confirm && void run(confirm)}
        onCancel={() => setConfirm(null)}
      />
    </form>
  );
}

export function ReportReview({ id }: { id: number }) {
  const { data: report, isPending, isError, error, refetch } = useAdminReport(id);

  if (isPending) {
    return (
      <div role="status" aria-label="Loading report" className="animate-pulse space-y-4">
        <div className="h-8 w-1/2 rounded bg-stone-200" />
        <div className="h-64 rounded-2xl bg-stone-200" />
      </div>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 404) {
      return (
        <EmptyState
          title="We could not find that report"
          action={
            <Link href="/dashboard/admin/reports" className={buttonClasses("primary")}>
              Back to reports
            </Link>
          }
        />
      );
    }

    return <ErrorState message={error.message} onRetry={() => refetch()} />;
  }

  const publicVisible = report.petStatus === "Available" && !report.petIsSuspended;

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-stone-600">
        <Link href="/dashboard/admin/reports" className="hover:underline">
          ← Reports
        </Link>
      </nav>

      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-stone-900">Report about {report.petName}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone={REPORT_STATUS_TONES[report.status]}>{REPORT_STATUS_LABELS[report.status]}</Badge>
          <Badge>{REASON_LABELS[report.reason]}</Badge>
          <span className="text-sm text-stone-500">Reported {formatDateTime(report.createdAtUtc)}</span>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-5">
        <div className="space-y-6 lg:col-span-3">
          <section className="rounded-2xl border border-stone-200 bg-white p-5">
            <h2 className="text-lg font-semibold text-stone-900">What was reported</h2>
            <dl className="mt-3 space-y-3 text-sm">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Reported by</dt>
                <dd className="mt-0.5 text-stone-900">{report.reporterName}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Details</dt>
                <dd className="mt-0.5 whitespace-pre-line text-stone-900">
                  {report.details ?? <span className="text-stone-500">No details were given.</span>}
                </dd>
              </div>
            </dl>
          </section>

          <section className="rounded-2xl border border-stone-200 bg-white p-5">
            <h2 className="text-lg font-semibold text-stone-900">The listing</h2>
            <div className="mt-3 space-y-3 text-sm">
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium text-stone-900">{report.petName}</span>
                <ListingBadges pet={{ status: report.petStatus, isSuspended: report.petIsSuspended }} />
              </div>
              <p className="text-stone-700">Listed by {report.ownerName}</p>
              <p className="flex flex-wrap gap-x-4">
                <Link href={`/dashboard/admin/pets?q=${encodeURIComponent(report.petName)}`} className="text-orange-700 hover:underline">
                  Find in listings
                </Link>
                <Link href={`/dashboard/admin/reports?petId=${report.petId}`} className="text-orange-700 hover:underline">
                  All reports for this listing
                </Link>
                {publicVisible && (
                  <Link href={`/pets/${report.petId}`} className="text-orange-700 hover:underline">
                    View the public page
                  </Link>
                )}
              </p>
              {report.status === "Open" && report.otherOpenReportsForPet > 0 && (
                <p className="rounded-lg bg-orange-50 px-3 py-2 text-orange-900">
                  {report.otherOpenReportsForPet} other open {report.otherOpenReportsForPet === 1 ? "report" : "reports"} about this
                  listing.
                </p>
              )}
            </div>
          </section>
        </div>

        <section aria-labelledby="review-heading" className="rounded-2xl border border-stone-200 bg-white p-5 lg:col-span-2">
          <h2 id="review-heading" className="text-lg font-semibold text-stone-900">
            {report.status === "Open" ? "Your review" : "Outcome"}
          </h2>

          {report.status === "Open" ? (
            <div className="mt-4">
              <ReviewForm key={report.status} report={report} />
            </div>
          ) : (
            <dl className="mt-3 space-y-3 text-sm">
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Reviewed by</dt>
                <dd className="mt-0.5 text-stone-900">
                  {report.reviewedBy ?? "Unknown"}
                  {report.reviewedAtUtc && <span className="text-stone-500"> · {formatDateTime(report.reviewedAtUtc)}</span>}
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Note</dt>
                <dd className="mt-0.5 whitespace-pre-line text-stone-900">{report.resolutionNote ?? "No note."}</dd>
              </div>
            </dl>
          )}
        </section>
      </div>
    </div>
  );
}