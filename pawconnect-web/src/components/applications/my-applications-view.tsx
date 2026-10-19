"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ApplicationCard } from "@/components/applications/application-card";
import { StatCard } from "@/components/dashboard/stat-card";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { buttonClasses } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useApplicantDashboard, useMyApplications } from "@/hooks/use-applications";
import { STATUS_LABELS, TYPE_LABELS } from "@/lib/applications/format";
import {
  APPLICATION_STATUSES,
  APPLICATION_TYPES,
  applicationsHref,
  hasApplicationFilters,
  type ApplicationFilters,
} from "@/lib/applications/filters";
import type { ApplicationStatus, ApplicationType } from "@/types/applications";

function ListSkeleton() {
  return (
    <ul role="status" aria-label="Loading applications" className="mt-4 animate-pulse space-y-3">
      {[0, 1, 2].map((i) => (
        <li key={i} className="h-28 rounded-2xl bg-stone-200" />
      ))}
    </ul>
  );
}

export function MyApplicationsView({ filters }: { filters: ApplicationFilters }) {
  const router = useRouter();
  const list = useMyApplications(filters);
  const summary = useApplicantDashboard();

  const go = (overrides: Partial<ApplicationFilters>) => router.push(applicationsHref(filters, overrides));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900">My applications</h1>
          <p className="mt-1 text-sm text-stone-600">Track every adoption and foster application in one place.</p>
        </div>
        <Link href="/pets" className={buttonClasses("secondary", "sm")}>
          Browse more pets
        </Link>
      </div>

      {summary.data && (
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard label="Total applications" value={summary.data.totalApplications} />
          <StatCard label="Active applications" value={summary.data.activeApplications} />
          <StatCard label="Unread notifications" value={summary.data.unreadNotifications} />
        </div>
      )}

      <form
        role="search"
        aria-label="Filter applications"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-3"
      >
        <FormField id="filter-status" label="Status">
          <Select
            id="filter-status"
            key={`status-${filters.status ?? ""}`}
            defaultValue={filters.status ?? ""}
            onChange={(event) => go({ status: (event.target.value || undefined) as ApplicationStatus | undefined })}
          >
            <option value="">Any status</option>
            {APPLICATION_STATUSES.map((value) => (
              <option key={value} value={value}>
                {STATUS_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="filter-type" label="Type">
          <Select
            id="filter-type"
            key={`type-${filters.type ?? ""}`}
            defaultValue={filters.type ?? ""}
            onChange={(event) => go({ type: (event.target.value || undefined) as ApplicationType | undefined })}
          >
            <option value="">Adoption and foster</option>
            {APPLICATION_TYPES.map((value) => (
              <option key={value} value={value}>
                {TYPE_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <div className="flex items-end">
          {hasApplicationFilters(filters) && (
            <Link href="/dashboard/my-applications" className={buttonClasses("secondary", "md")}>
              Reset filters
            </Link>
          )}
        </div>
      </form>

      {list.isError ? (
        <ErrorState message={list.error.message} onRetry={() => list.refetch()} />
      ) : list.isPending ? (
        <ListSkeleton />
      ) : list.data.totalCount === 0 ? (
        hasApplicationFilters(filters) ? (
          <EmptyState
            title="No applications match your filters"
            message="Try a different status or type."
            action={
              <Link href="/dashboard/my-applications" className={buttonClasses("secondary")}>
                Reset filters
              </Link>
            }
          />
        ) : (
          <EmptyState
            title="You have not applied yet"
            message="When you find a pet you would like to adopt or foster, your applications will appear here."
            action={
              <Link href="/pets" className={buttonClasses("primary")}>
                Browse pets
              </Link>
            }
          />
        )
      ) : list.data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          message={`There are only ${list.data.totalPages} pages.`}
          action={
            <Link href={applicationsHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={list.isPlaceholderData}>
          <p aria-live="polite" className="mb-3 text-sm text-stone-600">
            {list.data.totalCount} {list.data.totalCount === 1 ? "application" : "applications"}
          </p>
          <ul className="space-y-3">
            {list.data.items.map((application) => (
              <ApplicationCard key={application.id} application={application} />
            ))}
          </ul>
          <Pagination
            page={list.data.page}
            totalPages={list.data.totalPages}
            hrefFor={(target) => applicationsHref(filters, { page: target })}
          />
        </section>
      )}
    </div>
  );
}