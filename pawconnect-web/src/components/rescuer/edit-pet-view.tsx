"use client";

import Link from "next/link";
import { ListingBadges } from "@/components/rescuer/listing-badges";
import { PetForm } from "@/components/rescuer/pet-form";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { usePet } from "@/hooks/use-rescuer";
import { ApiError } from "@/lib/api-client";
import { LISTING_HELP_FOR } from "@/components/rescuer/listing-help";
import { PhotoManager } from "@/components/rescuer/photo-manager";


export function EditPetView({ id }: { id: number }) {
  const { data: pet, isPending, isError, error, refetch } = usePet(id);

  if (isPending) {
    return (
      <div role="status" aria-label="Loading listing" className="animate-pulse space-y-4">
        <div className="h-9 w-64 rounded bg-stone-200" />
        <div className="h-72 rounded-2xl bg-stone-200" />
      </div>
    );
  }

  if (isError) {
    if (error instanceof ApiError && error.status === 404) {
      return (
        <EmptyState
          title="We could not find that listing"
          message="It may have been removed, or it may belong to someone else."
          action={
            <Link href="/dashboard/rescuer/pets" className={buttonClasses("primary")}>
              Back to my listings
            </Link>
          }
        />
      );
    }

    return <ErrorState message={error.message} onRetry={() => refetch()} />;
  }

  const locked = pet.status === "Adopted" || pet.status === "Archived";

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-stone-600">
        <Link href="/dashboard/rescuer/pets" className="hover:underline">
          ← My listings
        </Link>
      </nav>

      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-bold text-stone-900">{pet.name}</h1>
        <ListingBadges pet={pet} />
      </div>

      <p className="text-sm text-stone-600">{LISTING_HELP_FOR(pet.status)}</p>

      {pet.isSuspended && (
        <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          <p className="font-semibold">This listing is suspended by moderation.</p>
          {pet.moderationNote && <p className="mt-1">Reason: {pet.moderationNote}</p>}
          <p className="mt-1">
            You can still update the details, but it cannot be published until an administrator restores it.
          </p>
        </div>
      )}

      {locked ? (
        <div role="status" className="rounded-2xl border border-stone-200 bg-white p-6 text-sm text-stone-700">
          {pet.status === "Adopted"
            ? "This pet has been adopted, so the listing can no longer be edited."
            : "This listing is archived and can no longer be edited."}
        </div>
      ) : (
        <>
          <PhotoManager petId={pet.id} petName={pet.name} />
          <PetForm key={pet.id} pet={pet} />
        </>
      )}
    </div>
  );
}