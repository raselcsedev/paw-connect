import Link from "next/link";
import { formatAge } from "@/lib/pets/format";
import { buildHref, type PetFilters } from "@/lib/pets/search";

export function ActiveFilters({ filters }: { filters: PetFilters }) {
  const chips: { label: string; href: string }[] = [];

  if (filters.q) chips.push({ label: `Search: “${filters.q}”`, href: buildHref(filters, { q: undefined }) });
  if (filters.species) chips.push({ label: `Species: ${filters.species}`, href: buildHref(filters, { species: undefined }) });
  if (filters.size) chips.push({ label: `Size: ${filters.size}`, href: buildHref(filters, { size: undefined }) });
  if (filters.availabilityType)
    chips.push({
      label: filters.availabilityType === "Adoption" ? "Adopt" : "Foster",
      href: buildHref(filters, { availabilityType: undefined }),
    });
  if (filters.city) chips.push({ label: `City: ${filters.city}`, href: buildHref(filters, { city: undefined }) });
  if (filters.minAgeMonths !== undefined)
    chips.push({ label: `From ${formatAge(filters.minAgeMonths)}`, href: buildHref(filters, { minAgeMonths: undefined }) });
  if (filters.maxAgeMonths !== undefined)
    chips.push({ label: `Up to ${formatAge(filters.maxAgeMonths)}`, href: buildHref(filters, { maxAgeMonths: undefined }) });
  if (filters.vaccinated) chips.push({ label: "Vaccinated", href: buildHref(filters, { vaccinated: undefined }) });

  if (chips.length === 0) return null;

  return (
    <div className="mt-4 flex flex-wrap items-center gap-2" aria-label="Active filters" role="group">
      {chips.map((chip) => (
        <Link
          key={chip.label}
          href={chip.href}
          aria-label={`Remove filter: ${chip.label}`}
          className="inline-flex items-center gap-1.5 rounded-full border border-orange-200 bg-orange-50 px-3 py-1 text-sm text-orange-900 hover:bg-orange-100 focus-visible:outline-2 focus-visible:outline-orange-600"
        >
          {chip.label}
          <span aria-hidden="true">×</span>
        </Link>
      ))}
      <Link href="/pets" className="text-sm font-medium text-stone-700 underline-offset-2 hover:underline">
        Clear all
      </Link>
    </div>
  );
}