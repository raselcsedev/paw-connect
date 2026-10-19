import Link from "next/link";
import { PetImage } from "@/components/pets/pet-image";
import { Badge } from "@/components/ui/badge";
import { availabilityLabel, formatAge, photoSrc, SPECIES_EMOJI } from "@/lib/pets/format";
import type { PetSummaryDto } from "@/types/pets";

export function PetCard({ pet }: { pet: PetSummaryDto }) {
  const src = pet.primaryPhotoId ? photoSrc(pet.id, pet.primaryPhotoId) : null;

  return (
    <li>
      <Link
        href={`/pets/${pet.id}`}
        className="group block overflow-hidden rounded-2xl border border-stone-200 bg-white shadow-sm transition hover:shadow-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange-600"
      >
        <div className="aspect-[4/3] overflow-hidden bg-orange-50">
          <PetImage
            src={src}
            alt={`Photo of ${pet.name}`}
            fallback={SPECIES_EMOJI[pet.species]}
            className="transition duration-300 group-hover:scale-105"
          />
        </div>
        <div className="space-y-2 p-4">
          <h3 className="text-lg font-semibold text-stone-900">{pet.name}</h3>
          <p className="text-sm text-stone-600">
            {pet.breed ?? pet.species} · {formatAge(pet.ageInMonths)}
          </p>
          <div className="flex flex-wrap gap-1.5">
            <Badge tone="orange">{availabilityLabel(pet.availabilityType)}</Badge>
            <Badge>{pet.size}</Badge>
          </div>
          <p className="text-sm text-stone-500">📍 {pet.city}</p>
        </div>
      </Link>
    </li>
  );
}