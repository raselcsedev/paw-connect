"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ListingBadges } from "@/components/rescuer/listing-badges";
import { ListingRowActions } from "@/components/rescuer/listing-row-actions";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { buttonClasses } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useMyPets } from "@/hooks/use-rescuer";
import { availabilityLabel, formatAge, formatDate } from "@/lib/pets/format";
import { LISTING_STATUSES, listingsHref, type ListingFilters } from "@/lib/rescuer/filters";
import { LISTING_LABELS } from "@/lib/rescuer/format";
import type { ListingStatus, PetDto } from "@/types/pets";

function ListingRow({ pet }: { pet: PetDto }) {
  return (
    <tr className="align-top">
      <td className="px-4 py-3">
        <p className="font-medium text-stone-900">{pet.name}</p>
        <p className="text-xs text-stone-500">
          {pet.breed ?? pet.species} · {formatAge(pet.ageInMonths)}
        </p>
        {pet.isSuspended && pet.moderationNote && (
          <p className="mt-1 max-w-xs text-xs text-red-700">Moderation note: {pet.moderationNote}</p>
        )}
      </td>
      <td className="px-4 py-3">
        <ListingBadges pet={pet} />
      </td>
      <td className="hidden px-4 py-3 text-stone-700 md:table-cell">{availabilityLabel(pet.availabilityType)}</td>
      <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{pet.area ? `${pet.area}, ${pet.city}` : pet.city}</td>
      <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{formatDate(pet.updatedAtUtc)}</td>
      <td className="px-4 py-3">
        <ListingRowActions pet={pet} />
      </td>
    </tr>
  );
}

export function ListingsView({ filters }: { filters: ListingFilters }) {
  const router = useRouter();
  const { data, isPending, isError, error, refetch, isPlaceholderData } = useMyPets(filters);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900">My listings</h1>
          <p className="mt-1 text-sm text-stone-600">Create, publish, pause and archive your pets.</p>
        </div>
        <Link href="/dashboard/rescuer/pets/new" className={buttonClasses("primary", "md")}>
          Create a listing
        </Link>
      </div>

      <form
        role="search"
        aria-label="Filter listings"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-3"
      >
        <FormField id="filter-listing-status" label="Status">
          <Select
            id="filter-listing-status"
            key={`status-${filters.status ?? ""}`}
            defaultValue={filters.status ?? ""}
            onChange={(event) =>
              router.push(listingsHref(filters, { status: (event.target.value || undefined) as ListingStatus | undefined }))
            }
          >
            <option value="">All listings</option>
            {LISTING_STATUSES.map((status) => (
              <option key={status} value={status}>
                {LISTING_LABELS[status]}
              </option>
            ))}
          </Select>
        </FormField>

        <div className="flex items-end">
          {filters.status && (
            <Link href="/dashboard/rescuer/pets" className={buttonClasses("secondary", "md")}>
              Reset filter
            </Link>
          )}
        </div>
      </form>

      {isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : isPending ? (
        <div role="status" aria-label="Loading listings" className="animate-pulse space-y-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-16 rounded-xl bg-stone-200" />
          ))}
        </div>
      ) : data.totalCount === 0 ? (
        filters.status ? (
          <EmptyState
            title={`No ${LISTING_LABELS[filters.status].toLowerCase()} listings`}
            action={
              <Link href="/dashboard/rescuer/pets" className={buttonClasses("secondary")}>
                Show all listings
              </Link>
            }
          />
        ) : (
          <EmptyState
            title="You have no listings yet"
            message="Create a listing, save it as a draft, and publish it when it is ready."
            action={
              <Link href="/dashboard/rescuer/pets/new" className={buttonClasses("primary")}>
                Create a listing
              </Link>
            }
          />
        )
      ) : data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          message={`There are only ${data.totalPages} pages.`}
          action={
            <Link href={listingsHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={isPlaceholderData}>
          <p aria-live="polite" className="mb-3 text-sm text-stone-600">
            {data.totalCount} {data.totalCount === 1 ? "listing" : "listings"}
          </p>
          <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white">
            <table className="w-full min-w-[32rem] text-left text-sm">
              <caption className="sr-only">Your pet listings</caption>
              <thead className="bg-stone-50 text-xs uppercase tracking-wide text-stone-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Pet</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Offered for</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Location</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Updated</th>
                  <th scope="col" className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((pet) => (
                  <ListingRow key={pet.id} pet={pet} />
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(target) => listingsHref(filters, { page: target })}
          />
        </section>
      )}
    </div>
  );
}