"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { StatusBadge } from "@/components/applications/status-badge";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useIncomingApplications, usePetOptions } from "@/hooks/use-review";
import { ApiError } from "@/lib/api-client";
import { APPLICATION_STATUSES, APPLICATION_TYPES } from "@/lib/applications/filters";
import { STATUS_LABELS, TYPE_LABELS } from "@/lib/applications/format";
import { formatDate } from "@/lib/pets/format";
import { hasIncomingFilters, incomingHref, type IncomingFilters } from "@/lib/rescuer/application-filters";
import type { ApplicationStatus, ApplicationType } from "@/types/applications";

export function IncomingView({ filters }: { filters: IncomingFilters }) {
  const router = useRouter();
  const list = useIncomingApplications(filters);
  const options = usePetOptions();

  const pets = options.data?.items ?? [];
  const go = (overrides: Partial<IncomingFilters>) => router.push(incomingHref(filters, overrides));

  const petNotFound =
    list.isError && list.error instanceof ApiError && list.error.status === 404 && filters.petId !== undefined;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Applications</h1>
        <p className="mt-1 text-sm text-stone-600">Everyone who applied for one of your pets.</p>
      </div>

      <form
        role="search"
        aria-label="Filter applications"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <FormField id="incoming-status" label="Status">
          <Select
            id="incoming-status"
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

        <FormField id="incoming-type" label="Type">
          <Select
            id="incoming-type"
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

        <FormField id="incoming-pet" label="Pet">
          <Select
            id="incoming-pet"
            key={`pet-${filters.petId ?? ""}-${pets.length}`}
            defaultValue={filters.petId ? String(filters.petId) : ""}
            onChange={(event) => go({ petId: event.target.value ? Number(event.target.value) : undefined })}
          >
            <option value="">All pets</option>
            {filters.petId !== undefined && !pets.some((pet) => pet.id === filters.petId) && (
              <option value={filters.petId}>Listing #{filters.petId}</option>
            )}
            {pets.map((pet) => (
              <option key={pet.id} value={pet.id}>
                {pet.name}
              </option>
            ))}
          </Select>
        </FormField>

        <div className="flex items-end">
          {hasIncomingFilters(filters) && (
            <Link href="/dashboard/rescuer/applications" className={buttonClasses("secondary", "md")}>
              Reset filters
            </Link>
          )}
        </div>
      </form>

      {petNotFound ? (
        <EmptyState
          title="We could not find that listing"
          message="It may not exist, or it may belong to someone else."
          action={
            <Link href="/dashboard/rescuer/applications" className={buttonClasses("primary")}>
              Show all applications
            </Link>
          }
        />
      ) : list.isError ? (
        <ErrorState message={list.error.message} onRetry={() => list.refetch()} />
      ) : list.isPending ? (
        <div role="status" aria-label="Loading applications" className="animate-pulse space-y-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-14 rounded-xl bg-stone-200" />
          ))}
        </div>
      ) : list.data.totalCount === 0 ? (
        hasIncomingFilters(filters) ? (
          <EmptyState
            title="No applications match your filters"
            action={
              <Link href="/dashboard/rescuer/applications" className={buttonClasses("secondary")}>
                Reset filters
              </Link>
            }
          />
        ) : (
          <EmptyState
            title="No applications yet"
            message="When someone applies for one of your published pets, they will appear here and you will be notified."
          />
        )
      ) : list.data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          message={`There are only ${list.data.totalPages} pages.`}
          action={
            <Link href={incomingHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={list.isPlaceholderData}>
          <p aria-live="polite" className="mb-3 text-sm text-stone-600">
            {list.data.totalCount} {list.data.totalCount === 1 ? "application" : "applications"}
          </p>

          <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white">
            <table className="w-full min-w-[28rem] text-left text-sm">
              <caption className="sr-only">Applications for your pets</caption>
              <thead className="bg-stone-50 text-xs uppercase tracking-wide text-stone-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Applicant</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium sm:table-cell">Pet</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Type</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Submitted</th>
                  <th scope="col" className="px-4 py-3 font-medium"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {list.data.items.map((application) => (
                  <tr key={application.id}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-stone-900">{application.applicantName}</p>
                      <p className="text-xs text-stone-500 sm:hidden">{application.petName}</p>
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 sm:table-cell">{application.petName}</td>
                    <td className="hidden px-4 py-3 md:table-cell">
                      <Badge>{TYPE_LABELS[application.type]}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={application.status} />
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{formatDate(application.createdAtUtc)}</td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        href={`/dashboard/rescuer/applications/${application.id}`}
                        aria-label={`Review the application from ${application.applicantName} for ${application.petName}`}
                        className={buttonClasses("secondary", "sm")}
                      >
                        Review
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination
            page={list.data.page}
            totalPages={list.data.totalPages}
            hrefFor={(target) => incomingHref(filters, { page: target })}
          />
        </section>
      )}
    </div>
  );
}