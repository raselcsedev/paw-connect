"use client";

import { useQuery } from "@tanstack/react-query";
import { StatCard } from "@/components/dashboard/stat-card";
import { ErrorState } from "@/components/ui/states";
import { useCurrentUser } from "@/hooks/use-auth";
import { apiFetch } from "@/lib/api-client";
import type { Role } from "@/types/api";

const endpoint: Record<Role, string> = {
  Adopter: "applicant",
  Rescuer: "rescuer",
  Admin: "admin",
};

function labelFor(key: string): string {
  return key.replace(/([A-Z])/g, " $1").replace(/^./, (c) => c.toUpperCase());
}

function summarize(data: Record<string, unknown>): { label: string; value: number }[] {
  const rows: { label: string; value: number }[] = [];

  for (const [key, value] of Object.entries(data)) {
    if (typeof value === "number") {
      rows.push({ label: labelFor(key), value });
    } else if (value && typeof value === "object" && "total" in value) {
      const total = (value as { total: unknown }).total;
      if (typeof total === "number") {
        rows.push({ label: `${labelFor(key)} total`, value: total });
      }
    }
  }

  return rows;
}

export default function DashboardPage() {
  const { data: user } = useCurrentUser();
  const role = user?.role;

  const overview = useQuery({
    queryKey: ["dashboard", role],
    enabled: role !== undefined,
    queryFn: () => apiFetch<Record<string, unknown>>(`/dashboard/${endpoint[role as Role]}`),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Welcome, {user?.displayName}</h1>
        <p className="mt-1 text-sm text-stone-600">
          Signed in as <span className="font-medium">{user?.role}</span>. Full dashboards arrive in the next lessons.
        </p>
      </div>

      {overview.isError ? (
        <ErrorState message={overview.error.message} onRetry={() => overview.refetch()} />
      ) : overview.isPending ? (
        <div className="grid animate-pulse gap-4 sm:grid-cols-2 lg:grid-cols-4" role="status" aria-label="Loading">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-28 rounded-xl bg-stone-200" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {summarize(overview.data).map((row) => (
            <StatCard key={row.label} label={row.label} value={row.value} />
          ))}
        </div>
      )}
    </div>
  );
}