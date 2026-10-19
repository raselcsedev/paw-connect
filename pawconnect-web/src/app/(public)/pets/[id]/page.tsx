import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { PhotoGallery } from "@/components/pets/photo-gallery";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { availabilityLabel, formatAge, formatDate, photoSrc, sexLabel, SPECIES_EMOJI } from "@/lib/pets/format";
import { getPet, getPhotos } from "@/lib/server/pets";
import type { PetDto } from "@/types/pets";

type Props = { params: Promise<{ id: string }> };

function parseId(value: string): number {
  if (!/^\d+$/.test(value)) notFound();

  const id = Number(value);
  if (!Number.isSafeInteger(id) || id < 1) notFound();

  return id;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const pet = await getPet(parseId((await params).id));

  return {
    title: `${pet.name}, ${pet.breed ?? pet.species} in ${pet.city}`,
    description: pet.description ? pet.description.slice(0, 160) : `Meet ${pet.name} on PawConnect.`,
  };
}

function yesNo(value: boolean): string {
  return value ? "Yes" : "No";
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">{label}</dt>
      <dd className="mt-0.5 text-sm font-medium text-stone-900">{value}</dd>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-stone-200 bg-white p-5">
      <h2 className="text-lg font-semibold text-stone-900">{title}</h2>
      <div className="mt-2 space-y-2 text-sm text-stone-700">{children}</div>
    </section>
  );
}

function ApplyActions({ pet }: { pet: PetDto }) {
  if (pet.status !== "Available") {
    return (
      <div role="status" className="rounded-xl bg-stone-100 p-4 text-sm text-stone-700">
        {pet.status === "Adopted"
          ? `${pet.name} has found a home. Thank you to everyone who applied!`
          : `${pet.name} is not accepting applications right now.`}
        <div className="mt-3">
          <Link href="/pets" className={buttonClasses("secondary", "sm")}>
            See other pets
          </Link>
        </div>
      </div>
    );
  }

  const canAdopt = pet.availabilityType !== "Foster";
  const canFoster = pet.availabilityType !== "Adoption";

  return (
    <div className="flex flex-col gap-2 sm:flex-row lg:flex-col">
      {canAdopt && (
        <Link href={`/pets/${pet.id}/apply?type=Adoption`} className={buttonClasses("primary", "lg", "w-full")}>
          Apply to adopt
        </Link>
      )}
      {canFoster && (
        <Link
          href={`/pets/${pet.id}/apply?type=Foster`}
          className={buttonClasses(canAdopt ? "secondary" : "primary", "lg", "w-full")}
        >
          Apply to foster
        </Link>
      )}
    </div>
  );
}

export default async function PetDetailPage({ params }: Props) {
  const id = parseId((await params).id);
  const [pet, photos] = await Promise.all([getPet(id), getPhotos(id)]);

  const location = pet.area ? `${pet.area}, ${pet.city}` : pet.city;

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8">
      <nav aria-label="Breadcrumb" className="mb-4 text-sm text-stone-600">
        <Link href="/pets" className="hover:underline">
          ← All pets
        </Link>
      </nav>

      <div className="grid gap-8 lg:grid-cols-5">
        <div className="lg:col-span-3">
          <PhotoGallery
            name={pet.name}
            fallbackEmoji={SPECIES_EMOJI[pet.species]}
            photos={photos.map((photo) => ({ id: photo.id, src: photoSrc(pet.id, photo.id) }))}
          />
        </div>

        <aside className="space-y-4 lg:col-span-2">
          <div>
            <h1 className="text-3xl font-bold text-stone-900">{pet.name}</h1>
            <p className="mt-1 text-stone-600">
              {pet.breed ?? pet.species} · {formatAge(pet.ageInMonths)}
            </p>
            <div className="mt-3 flex flex-wrap gap-1.5">
              <Badge tone="orange">{availabilityLabel(pet.availabilityType)}</Badge>
              <Badge>{pet.size}</Badge>
              {pet.isVaccinated && <Badge tone="green">Vaccinated</Badge>}
              {pet.isSpayedNeutered && <Badge tone="green">Spayed / neutered</Badge>}
              {pet.status !== "Available" && <Badge tone="red">{pet.status === "Adopted" ? "Adopted" : "Unavailable"}</Badge>}
            </div>
          </div>

          <ApplyActions pet={pet} />

          <dl className="grid grid-cols-2 gap-4 rounded-2xl border border-stone-200 bg-white p-5">
            <Fact label="Species" value={pet.species} />
            <Fact label="Sex" value={sexLabel(pet.sex)} />
            <Fact label="Size" value={pet.size} />
            <Fact label="Age" value={formatAge(pet.ageInMonths)} />
            <Fact label="Location" value={location} />
            <Fact label="Listed" value={formatDate(pet.createdAtUtc)} />
          </dl>

          {pet.rescuerName && (
            <div className="rounded-2xl border border-stone-200 bg-white p-5 text-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-stone-500">Listed by</p>
              <p className="mt-0.5 font-medium text-stone-900">{pet.rescuerName}</p>
              <p className="mt-1 text-stone-600">Contact the rescuer through your application.</p>
            </div>
          )}
        </aside>
      </div>

      <div className="mt-8 grid gap-4 lg:grid-cols-2">
        <Section title={`About ${pet.name}`}>
          <p className="whitespace-pre-line">{pet.description ?? "The rescuer has not added a description yet."}</p>
        </Section>

        <Section title="Health and care">
          <dl className="grid grid-cols-2 gap-4">
            <Fact label="Vaccinated" value={yesNo(pet.isVaccinated)} />
            <Fact label="Spayed / neutered" value={yesNo(pet.isSpayedNeutered)} />
          </dl>
          {pet.healthStatus && <p>{pet.healthStatus}</p>}
          {pet.specialCareRequirements ? (
            <p>
              <span className="font-medium text-stone-900">Special care: </span>
              {pet.specialCareRequirements}
            </p>
          ) : (
            <p className="text-stone-500">No special care requirements listed.</p>
          )}
        </Section>

        {pet.requirements && (
          <Section title={pet.availabilityType === "Foster" ? "Foster requirements" : "Adoption requirements"}>
            <p className="whitespace-pre-line">{pet.requirements}</p>
          </Section>
        )}
      </div>
    </div>
  );
}