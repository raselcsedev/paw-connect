"use client";

import Link from "next/link";
import { BreakdownList } from "@/components/dashboard/breakdown-list";
import { StatCard } from "@/components/dashboard/stat-card";
import { Button, buttonClasses } from "@/components/ui/button";
import { ErrorState } from "@/components/ui/states";
import { useAdminDashboard } from "@/hooks/use-admin";
import { APPLICATION_STATUSES, APPLICATION_TYPES } from "@/lib/applications/filters";
import { formatDateTime, STATUS_LABELS, TYPE_LABELS } from "@/lib/applications/format";
import { ACCOUNT_STATUSES, USER_ROLES } from "@/lib/admin/filters";
import { LISTING_STATUSES } from "@/lib/rescuer/filters";
import { LISTING_LABELS } from "@/lib/rescuer/format";

export function AdminOverview() {
  const { data, isPending, isError, error, refetch, isFetching } = useAdminDashboard();

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900">Administration</h1>
          <p className="mt-1 text-sm text-stone-600">
            {data ? `Statistics as of ${formatDateTime(data.generatedAtUtc)}.` : "Platform statistics and moderation."}
          </p>
        </div>
        <Button variant="secondary" size="sm" loading={isFetching && !isPending} onClick={() => refetch()}>
          Refresh
        </Button>
      </div>

      {isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : isPending ? (
        <div role="status" aria-label="Loading statistics" className="animate-pulse space-y-4">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="h-28 rounded-xl bg-stone-200" />
            ))}
          </div>
          <div className="h-64 rounded-2xl bg-stone-200" />
        </div>
      ) : (
        <>
          {data.openReports > 0 && (
            <div role="status" className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-orange-200 bg-orange-50 px-4 py-3 text-sm text-orange-900">
              <span>
                {data.openReports} {data.openReports === 1 ? "report is" : "reports are"} waiting for review.
              </span>
              <Link href="/dashboard/admin/reports?status=Open" className={buttonClasses("primary", "sm")}>
                Open the queue
              </Link>
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard label="Users" value={data.users.total} />
            <StatCard label="Listings" value={data.pets.total} />
            <StatCard label="Applications" value={data.applications.total} />
            <StatCard label="Open reports" value={data.openReports} />
            <StatCard label="New users (7 days)" value={data.users.newLast7Days} />
            <StatCard label="New users (30 days)" value={data.users.newLast30Days} />
            <StatCard label="Completed adoptions" value={data.applications.completedAdoptions} />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <BreakdownList
              title="Users by role"
              entries={USER_ROLES.map((role) => ({ label: role, value: data.users.byRole[role] }))}
            />
            <BreakdownList
              title="Accounts by status"
              entries={ACCOUNT_STATUSES.map((status) => ({ label: status, value: data.users.byStatus[status] }))}
            />
            <BreakdownList
              title="Listings by status"
              entries={LISTING_STATUSES.map((status) => ({ label: LISTING_LABELS[status], value: data.pets.byStatus[status] }))}
            />
            <BreakdownList
              title="Applications by status"
              entries={APPLICATION_STATUSES.map((status) => ({
                label: STATUS_LABELS[status],
                value: data.applications.byStatus[status],
              }))}
            />
            <BreakdownList
              title="Applications by type"
              entries={APPLICATION_TYPES.map((type) => ({ label: TYPE_LABELS[type], value: data.applications.byType[type] }))}
            />
          </div>
        </>
      )}
    </div>
  );
}