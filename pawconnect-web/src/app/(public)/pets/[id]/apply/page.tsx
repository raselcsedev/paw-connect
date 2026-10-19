import type { Metadata } from "next";
import Link from "next/link";
import { ApplicationForm } from "@/components/applications/application-form";
import { RequireAuth } from "@/components/auth/require-auth";
import { PetImage } from "@/components/pets/pet-image";
import { buttonClasses } from "@/components/ui/button";
import { allowedTypes } from "@/lib/applications/rules";
import { availabilityLabel, formatAge, photoSrc, SPECIES_EMOJI } from "@/lib/pets/format";
import { parsePetId } from "@/lib/pets/id";
import { getPet, getPhotos } from "@/lib/server/pets";

type Props = {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ type?: string | string[] }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const pet = await getPet(parsePetId((await params).id));
  return { title: `Apply for ${pet.name}`, robots: { index: false, follow: false } };
}

export default async function ApplyPage({ params, searchParams }: Props) {
  const id = parsePetId((await params).id);
  const [pet, photos, query] = await Promise.all([getPet(id), getPhotos(id), searchParams]);

  const allowed = allowedTypes(pet.availabilityType);
  const requested = typeof query.type === "string" ? query.type : undefined;
  const initialType = allowed.find((value) => value === requested) ?? allowed[0];

  const cover = photos.find((photo) => photo.isPrimary) ?? photos[0];

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <nav aria-label="Breadcrumb" className="mb-4 text-sm text-stone-600">
        <Link href={`/pets/${pet.id}`} className="hover:underline">
          ← Back to {pet.name}
        </Link>
      </nav>

      <h1 className="text-3xl font-bold text-stone-900">Apply for {pet.name}</h1>

      <RequireAuth roles={["Adopter"]}>
        {pet.status !== "Available" ? (
          <div role="status" className="mt-6 rounded-2xl border border-stone-200 bg-white p-6">
            <p className="text-stone-700">
              {pet.name} is not accepting applications right now. You can look at other pets who need a home.
            </p>
            <Link href="/pets" className={`${buttonClasses("primary")} mt-4`}>
              Browse pets
            </Link>
          </div>
        ) : (
          <div className="mt-6 grid gap-8 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <ApplicationForm
                petId={pet.id}
                petName={pet.name}
                allowedTypes={allowed}
                initialType={initialType}
              />
            </div>

            <aside className="space-y-4 lg:sticky lg:top-24 lg:self-start">
              <div className="overflow-hidden rounded-2xl border border-stone-200 bg-white">
                <div className="aspect-[4/3] bg-orange-50">
                  <PetImage
                    src={cover ? photoSrc(pet.id, cover.id) : null}
                    alt={`Photo of ${pet.name}`}
                    fallback={SPECIES_EMOJI[pet.species]}
                    eager
                  />
                </div>
                <div className="space-y-1 p-4">
                  <p className="text-lg font-semibold text-stone-900">{pet.name}</p>
                  <p className="text-sm text-stone-600">
                    {pet.breed ?? pet.species} · {formatAge(pet.ageInMonths)} · {pet.city}
                  </p>
                  <p className="text-sm text-stone-600">Offered for: {availabilityLabel(pet.availabilityType)}</p>
                  {pet.rescuerName && <p className="text-sm text-stone-600">Listed by {pet.rescuerName}</p>}
                </div>
              </div>

              {pet.requirements && (
                <div className="rounded-2xl border border-orange-200 bg-orange-50 p-4 text-sm text-orange-950">
                  <p className="font-semibold">The rescuer asks for</p>
                  <p className="mt-1 whitespace-pre-line">{pet.requirements}</p>
                </div>
              )}
            </aside>
          </div>
        )}
      </RequireAuth>
    </div>
  );
}