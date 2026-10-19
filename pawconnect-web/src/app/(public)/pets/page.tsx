import type { Metadata } from "next";
import { Suspense } from "react";
import { ActiveFilters } from "@/components/pets/active-filters";
import { PetFilterBar } from "@/components/pets/pet-filters";
import { PetGridSkeleton } from "@/components/pets/pet-grid-skeleton";
import { PetResults } from "@/components/pets/pet-results";
import { parseFilters, toQueryString, type RawSearchParams } from "@/lib/pets/search";

export const metadata: Metadata = {
  title: "Browse pets",
  description: "Search pets that need a home or a foster family.",
};

export default async function PetsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const filters = parseFilters(await searchParams);

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <h1 className="text-3xl font-bold text-stone-900">Find your new companion</h1>
      <p className="mt-1 text-stone-600">Filter by species, size, age and more. Every search has its own shareable link.</p>

      <div className="mt-6">
        <PetFilterBar filters={filters} />
      </div>

      <ActiveFilters filters={filters} />

      <Suspense key={toQueryString(filters)} fallback={<PetGridSkeleton />}>
        <PetResults filters={filters} />
      </Suspense>
    </div>
  );
}