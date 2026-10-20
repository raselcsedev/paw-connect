"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useSubmitReport } from "@/hooks/use-admin";
import { useCurrentUser } from "@/hooks/use-auth";
import { REPORT_REASONS } from "@/lib/admin/filters";
import { REASON_LABELS } from "@/lib/admin/format";
import { applyServerErrors } from "@/lib/forms";
import { reportSchema, type ReportValues } from "@/lib/validations/moderation";

function ReportForm({ petId, petName, onDone }: { petId: number; petName: string; onDone: () => void }) {
  const submit = useSubmitReport();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ReportValues>({ resolver: zodResolver(reportSchema), defaultValues: { details: "" } });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await submit.mutateAsync({ petId, reason: values.reason, details: values.details || undefined });
      toast.success("Thank you. A moderator will review your report.");
      onDone();
    } catch (error) {
      const message = applyServerErrors(error, setError, ["reason", "details"]);
      if (message) setError("root.serverError", { type: "server", message });
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      <p className="text-sm text-stone-600">
        Tell us what is wrong with the listing for {petName}. Reports are reviewed by our moderators. Please do not include
        personal information.
      </p>

      {errors.root?.serverError && (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {errors.root.serverError.message}
        </p>
      )}

      <FormField id="report-reason" label="Reason" error={errors.reason?.message}>
        <Select
          id="report-reason"
          defaultValue=""
          aria-invalid={errors.reason ? true : undefined}
          aria-describedby={errors.reason ? "report-reason-error" : undefined}
          {...register("reason")}
        >
          <option value="" disabled>
            Choose a reason
          </option>
          {REPORT_REASONS.map((reason) => (
            <option key={reason} value={reason}>
              {REASON_LABELS[reason]}
            </option>
          ))}
        </Select>
      </FormField>

      <FormField id="report-details" label="Details (optional unless you chose Other)" error={errors.details?.message}>
        <Textarea
          id="report-details"
          rows={4}
          maxLength={1000}
          invalid={!!errors.details}
          aria-describedby={errors.details ? "report-details-error" : undefined}
          {...register("details")}
        />
      </FormField>

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onDone} disabled={submit.isPending}>
          Cancel
        </Button>
        <Button type="submit" loading={submit.isPending}>
          Send report
        </Button>
      </div>
    </form>
  );
}

export function ReportListingButton({ petId, petName }: { petId: number; petName: string }) {
  const { data: user, isPending } = useCurrentUser();
  const [open, setOpen] = useState(false);

  if (isPending) return null;

  if (!user) {
    return (
      <p className="text-sm text-stone-600">
        <Link href={`/login?next=${encodeURIComponent(`/pets/${petId}`)}`} className="underline">
          Sign in
        </Link>{" "}
        to report this listing.
      </p>
    );
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="text-sm text-stone-600 underline underline-offset-2 hover:text-stone-900 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange-600"
      >
        Report this listing
      </button>
      <Modal open={open} title={`Report ${petName}`} onClose={() => setOpen(false)}>
        <ReportForm petId={petId} petName={petName} onDone={() => setOpen(false)} />
      </Modal>
    </>
  );
}