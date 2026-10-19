import Link from "next/link";
import { PetCard } from "@/components/pets/pet-card";
import { Pagination } from "@/components/pets/pagination";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";
import {
  hasActiveFilters,
  buildHref,
  type PetFilters,
} from "@/lib/pets/search";
import { searchPets } from "@/lib/server/pets";

export async function PetResults({ filters }: { filters: PetFilters }) {
  const result = await searchPets(filters);

  if (result.totalCount === 0) {
    return hasActiveFilters(filters) ? (
      <div className="mt-6">
        <EmptyState
          title="No pets match your filters"
          message="Try removing a filter or searching for something broader."
          action={
            <Link href="/pets" className={buttonClasses("secondary")}>
              Reset filters
            </Link>
          }
        />
      </div>
    ) : (
      <div className="mt-6">
        <EmptyState
          title="No pets are listed yet"
          message="New pets are added by rescuers all the time. Please check back soon."
        />
      </div>
    );
  }

  if (result.items.length === 0) {
    return (
      <div className="mt-6">
        <EmptyState
          title="That page does not exist"
          message={`There are only ${result.totalPages} pages of results.`}
          action={
            <Link
              href={buildHref(filters, { page: 1 })}
              className={buttonClasses("secondary")}
            >
              Go to the first page
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <section aria-labelledby="results-heading" className="mt-4">
      <h2 id="results-heading" className="sr-only">
        Results
      </h2>
      <p aria-live="polite" className="text-sm text-stone-600">
        {result.totalCount} {result.totalCount === 1 ? "pet" : "pets"} found
      </p>
      <ul className="mt-4 grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {result.items.map((pet) => (
          <PetCard key={pet.id} pet={pet} />
        ))}
      </ul>
      <Pagination
        page={result.page}
        totalPages={result.totalPages}
        hrefFor={(target) => buildHref(filters, { page: target })}
      />
    </section>
  );
}
