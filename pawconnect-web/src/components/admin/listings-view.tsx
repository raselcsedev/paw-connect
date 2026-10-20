"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { NoteDialog } from "@/components/admin/note-dialog";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { ListingBadges } from "@/components/rescuer/listing-badges";
import { Button, buttonClasses } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useAdminPets, useModeratePet } from "@/hooks/use-admin";
import { adminPetsHref, hasAdminPetFilters, type AdminPetFilters } from "@/lib/admin/filters";
import { errorMessage } from "@/lib/errors";
import { formatDate } from "@/lib/pets/format";
import { LISTING_STATUSES } from "@/lib/rescuer/filters";
import { LISTING_LABELS } from "@/lib/rescuer/format";
import type { AdminPetDto } from "@/types/admin";
import type { ListingStatus } from "@/types/pets";

type Kind = "Suspend" | "Restore" | "Archive";

const COPY: Record<Kind, { title: string; message: (name: string) => string; label: string; variant: "danger" | "primary"; done: string }> = {
  Suspend: {
    title: "Suspend this listing?",
    message: (name) => `${name} is hidden from the public and the owner cannot publish it until you restore it. The owner is notified and sees your note.`,
    label: "Suspend",
    variant: "danger",
    done: "Listing suspended.",
  },
  Restore: {
    title: "Restore this listing?",
    message: (name) => `The suspension on ${name} is lifted. The listing stays paused, and the owner decides when to publish it again. The owner is notified.`,
    label: "Restore",
    variant: "primary",
    done: "Suspension lifted.",
  },
  Archive: {
    title: "Remove this listing?",
    message: (name) => `${name} is archived. Every open application for it is rejected, and those applicants and the owner are notified. This cannot be undone.`,
    label: "Archive listing",
    variant: "danger",
    done: "Listing archived.",
  },
};

function ModerationActions({ pet }: { pet: AdminPetDto }) {
  const moderate = useModeratePet();
  const [kind, setKind] = useState<Kind | null>(null);

  const canSuspend = !pet.isSuspended && pet.status !== "Adopted" && pet.status !== "Archived";
  const canRestore = pet.isSuspended;
  const canArchive = pet.status !== "Archived";

  async function confirm(note: string) {
    if (!kind) return;

    try {
      await moderate.mutateAsync({ id: pet.id, action: kind, note });
      toast.success(COPY[kind].done);
      setKind(null);
    } catch (error) {
      toast.error(errorMessage(error));
      setKind(null);
    }
  }

  const copy = kind ? COPY[kind] : COPY.Suspend;

  return (
    <div className="flex flex-wrap gap-2">
      {canSuspend && <Button size="sm" variant="secondary" onClick={() => setKind("Suspend")}>Suspend</Button>}
      {canRestore && <Button size="sm" onClick={() => setKind("Restore")}>Restore</Button>}
      {canArchive && <Button size="sm" variant="ghost" onClick={() => setKind("Archive")}>Archive</Button>}
      {!canSuspend && !canRestore && !canArchive && <span className="text-xs text-stone-500">No actions</span>}

      <NoteDialog
        open={kind !== null}
        title={`${copy.title.replace("this listing", pet.name)}`}
        message={copy.message(pet.name)}
        confirmLabel={copy.label}
        confirmVariant={copy.variant}
        loading={moderate.isPending}
        onConfirm={(note) => void confirm(note)}
        onCancel={() => setKind(null)}
      />
    </div>
  );
}

export function AdminListingsView({ filters }: { filters: AdminPetFilters }) {
  const router = useRouter();
  const { data, isPending, isError, error, refetch, isPlaceholderData } = useAdminPets(filters);
  const go = (overrides: Partial<AdminPetFilters>) => router.push(adminPetsHref(filters, overrides));

  function commitSearch(raw: string) {
    const value = raw.trim();
    if (value !== (filters.q ?? "")) go({ q: value || undefined });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Listings</h1>
        <p className="mt-1 text-sm text-stone-600">Every listing, including drafts and suspended ones. Actions are logged and the owner is notified.</p>
      </div>

      <form
        role="search"
        aria-label="Filter listings"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <FormField id="admin-pet-q" label="Search by name or breed">
          <Input
            id="admin-pet-q"
            key={`q-${filters.q ?? ""}`}
            type="search"
            defaultValue={filters.q ?? ""}
            maxLength={100}
            onBlur={(event) => commitSearch(event.target.value)}
            onKeyDown={(event) => event.key === "Enter" && event.currentTarget.blur()}
          />
        </FormField>

        <FormField id="admin-pet-status" label="Status">
          <Select
            id="admin-pet-status"
            key={`status-${filters.status ?? ""}`}
            defaultValue={filters.status ?? ""}
            onChange={(event) => go({ status: (event.target.value || undefined) as ListingStatus | undefined })}
          >
            <option value="">Any status</option>
            {LISTING_STATUSES.map((value) => (
              <option key={value} value={value}>
                {LISTING_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="admin-pet-suspended" label="Moderation">
          <Select
            id="admin-pet-suspended"
            key={`suspended-${String(filters.suspended)}`}
            defaultValue={filters.suspended === undefined ? "" : String(filters.suspended)}
            onChange={(event) => go({ suspended: event.target.value === "" ? undefined : event.target.value === "true" })}
          >
            <option value="">All</option>
            <option value="true">Suspended</option>
            <option value="false">Not suspended</option>
          </Select>
        </FormField>

        <div className="flex items-end">
          {hasAdminPetFilters(filters) && (
            <Link href="/dashboard/admin/pets" className={buttonClasses("secondary", "md")}>
              Reset filters
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
        <EmptyState
          title="No listings match your filters"
          action={
            <Link href="/dashboard/admin/pets" className={buttonClasses("secondary")}>
              Reset filters
            </Link>
          }
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          action={
            <Link href={adminPetsHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
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
              <caption className="sr-only">Listings</caption>
              <thead className="bg-stone-50 text-xs uppercase tracking-wide text-stone-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Listing</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Owner</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Open reports</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Updated</th>
                  <th scope="col" className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((pet) => (
                  <tr key={pet.id} className="align-top">
                    <td className="px-4 py-3">
                      <p className="font-medium text-stone-900">{pet.name}</p>
                      <p className="text-xs text-stone-500">{pet.species}</p>
                      {pet.isSuspended && pet.moderationNote && (
                        <p className="mt-1 max-w-xs text-xs text-red-700">Note: {pet.moderationNote}</p>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <ListingBadges pet={pet} />
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 md:table-cell">{pet.ownerName}</td>
                    <td className="hidden px-4 py-3 md:table-cell">
                      {pet.openReports > 0 ? (
                        <Link href={`/dashboard/admin/reports?petId=${pet.id}&status=Open`} className="font-medium text-orange-700 hover:underline">
                          {pet.openReports}
                        </Link>
                      ) : (
                        <span className="text-stone-500">0</span>
                      )}
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{formatDate(pet.updatedAtUtc)}</td>
                    <td className="px-4 py-3">
                      <ModerationActions pet={pet} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(target) => adminPetsHref(filters, { page: target })} />
        </section>
      )}
    </div>
  );
}