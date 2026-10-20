"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useAdminReports } from "@/hooks/use-admin";
import { hasReportFilters, REPORT_REASONS, REPORT_STATUSES, reportsHref, type ReportFilters } from "@/lib/admin/filters";
import { REASON_LABELS, REPORT_STATUS_LABELS, REPORT_STATUS_TONES } from "@/lib/admin/format";
import { formatDate } from "@/lib/pets/format";
import type { ReportReason, ReportStatus } from "@/types/admin";

export function ReportsView({ filters }: { filters: ReportFilters }) {
  const router = useRouter();
  const { data, isPending, isError, error, refetch, isPlaceholderData } = useAdminReports(filters);
  const go = (overrides: Partial<ReportFilters>) => router.push(reportsHref(filters, overrides));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Reports</h1>
        <p className="mt-1 text-sm text-stone-600">Open reports come first. Open one to review it.</p>
      </div>

      <form
        role="search"
        aria-label="Filter reports"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <FormField id="report-status" label="Status">
          <Select
            id="report-status"
            key={`status-${filters.status ?? ""}`}
            defaultValue={filters.status ?? ""}
            onChange={(event) => go({ status: (event.target.value || undefined) as ReportStatus | undefined })}
          >
            <option value="">Any status</option>
            {REPORT_STATUSES.map((value) => (
              <option key={value} value={value}>
                {REPORT_STATUS_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="report-reason-filter" label="Reason">
          <Select
            id="report-reason-filter"
            key={`reason-${filters.reason ?? ""}`}
            defaultValue={filters.reason ?? ""}
            onChange={(event) => go({ reason: (event.target.value || undefined) as ReportReason | undefined })}
          >
            <option value="">Any reason</option>
            {REPORT_REASONS.map((value) => (
              <option key={value} value={value}>
                {REASON_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <div className="flex flex-wrap items-end gap-2 lg:col-span-2">
          {filters.petId && (
            <Link href={reportsHref(filters, { petId: undefined })} className="inline-flex items-center gap-1.5 rounded-full border border-orange-200 bg-orange-50 px-3 py-1.5 text-sm text-orange-900 hover:bg-orange-100">
              Listing #{filters.petId} <span aria-hidden="true">×</span>
              <span className="sr-only">Remove listing filter</span>
            </Link>
          )}
          {hasReportFilters(filters) && (
            <Link href="/dashboard/admin/reports" className={buttonClasses("secondary", "md")}>
              Reset filters
            </Link>
          )}
        </div>
      </form>

      {isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : isPending ? (
        <div role="status" aria-label="Loading reports" className="animate-pulse space-y-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-14 rounded-xl bg-stone-200" />
          ))}
        </div>
      ) : data.totalCount === 0 ? (
        <EmptyState
          title={hasReportFilters(filters) ? "No reports match your filters" : "No reports yet"}
          message={hasReportFilters(filters) ? undefined : "When someone reports a listing, it will appear here."}
          action={
            hasReportFilters(filters) ? (
              <Link href="/dashboard/admin/reports" className={buttonClasses("secondary")}>
                Reset filters
              </Link>
            ) : undefined
          }
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          action={
            <Link href={reportsHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={isPlaceholderData}>
          <p aria-live="polite" className="mb-3 text-sm text-stone-600">
            {data.totalCount} {data.totalCount === 1 ? "report" : "reports"}
          </p>
          <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white">
            <table className="w-full min-w-[28rem] text-left text-sm">
              <caption className="sr-only">Reports</caption>
              <thead className="bg-stone-50 text-xs uppercase tracking-wide text-stone-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Listing</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium sm:table-cell">Reason</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Reported by</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Reported</th>
                  <th scope="col" className="px-4 py-3 font-medium"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((report) => (
                  <tr key={report.id}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-stone-900">{report.petName}</p>
                      <p className="text-xs text-stone-500 sm:hidden">{REASON_LABELS[report.reason]}</p>
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 sm:table-cell">{REASON_LABELS[report.reason]}</td>
                    <td className="hidden px-4 py-3 text-stone-700 md:table-cell">{report.reporterName}</td>
                    <td className="px-4 py-3">
                      <Badge tone={REPORT_STATUS_TONES[report.status]}>{REPORT_STATUS_LABELS[report.status]}</Badge>
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{formatDate(report.createdAtUtc)}</td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        href={`/dashboard/admin/reports/${report.id}`}
                        aria-label={`${report.status === "Open" ? "Review" : "View"} the report about ${report.petName}`}
                        className={buttonClasses("secondary", "sm")}
                      >
                        {report.status === "Open" ? "Review" : "View"}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(target) => reportsHref(filters, { page: target })} />
        </section>
      )}
    </div>
  );
}