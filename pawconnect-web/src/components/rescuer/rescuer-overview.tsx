"use client";

import Link from "next/link";
import { StatusBadge } from "@/components/applications/status-badge";
import { BreakdownList } from "@/components/dashboard/breakdown-list";
import { StatCard } from "@/components/dashboard/stat-card";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useRescuerDashboard } from "@/hooks/use-rescuer";
import { APPLICATION_STATUSES } from "@/lib/applications/filters";
import { STATUS_LABELS, TYPE_LABELS } from "@/lib/applications/format";
import { formatDate } from "@/lib/pets/format";
import { LISTING_STATUSES } from "@/lib/rescuer/filters";
import { LISTING_LABELS } from "@/lib/rescuer/format";

export function RescuerOverview() {
  const { data, isPending, isError, error, refetch } = useRescuerDashboard();

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900">Rescuer dashboard</h1>
          <p className="mt-1 text-sm text-stone-600">Your listings and the applications waiting for you.</p>
        </div>
        <Link href="/dashboard/rescuer/pets/new" className={buttonClasses("primary", "md")}>
          Create a listing
        </Link>
      </div>

      {isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : isPending ? (
        <div role="status" aria-label="Loading dashboard" className="animate-pulse space-y-4">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {[0, 1, 2, 3, 4].map((i) => (
              <div key={i} className="h-28 rounded-xl bg-stone-200" />
            ))}
          </div>
          <div className="h-64 rounded-2xl bg-stone-200" />
        </div>
      ) : data.totalPets === 0 ? (
        <EmptyState
          title="Create your first listing"
          message="Add a pet, save it as a draft, then publish it when the photos and details are ready."
          action={
            <Link href="/dashboard/rescuer/pets/new" className={buttonClasses("primary")}>
              Create a listing
            </Link>
          }
        />
      ) : (
        <>
          {data.awaitingCompletion > 0 && (
            <div role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
              {data.awaitingCompletion} approved {data.awaitingCompletion === 1 ? "adoption is" : "adoptions are"} waiting for you
              to complete the handover.
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            <StatCard label="Listings" value={data.totalPets} />
            <StatCard label="Awaiting review" value={data.awaitingReview} />
            <StatCard label="Ready to complete" value={data.awaitingCompletion} />
            <StatCard label="Completed adoptions" value={data.completedAdoptions} />
            <StatCard label="Unread notifications" value={data.unreadNotifications} />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <BreakdownList
              title="Listings by status"
              entries={LISTING_STATUSES.map((status) => ({ label: LISTING_LABELS[status], value: data.petsByStatus[status] }))}
            />
            <BreakdownList
              title="Applications by status"
              entries={APPLICATION_STATUSES.map((status) => ({
                label: STATUS_LABELS[status],
                value: data.applicationsByStatus[status],
              }))}
            />
          </div>

          <section aria-labelledby="attention-heading" className="rounded-2xl border border-stone-200 bg-white p-5">
            <h2 id="attention-heading" className="text-base font-semibold text-stone-900">
              Needs your attention
            </h2>

            {data.recentOpen.length === 0 ? (
              <p className="mt-3 text-sm text-stone-600">No applications are waiting for a decision. Nicely done.</p>
            ) : (
              <ul className="mt-3 divide-y divide-stone-100">
                {data.recentOpen.map((application) => (
                  <li key={application.id} className="flex flex-wrap items-center justify-between gap-3 py-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-stone-900">
                        {application.applicantName} applied for {application.petName}
                      </p>
                      <p className="mt-0.5 text-xs text-stone-500">Submitted {formatDate(application.createdAtUtc)}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge>{TYPE_LABELS[application.type]}</Badge>
                      <StatusBadge status={application.status} />
                      <Link
                        href={`/dashboard/rescuer/applications/${application.id}`}
                        className={buttonClasses("secondary", "sm")}
                      >
                        Review
                      </Link>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}
    </div>
  );
}