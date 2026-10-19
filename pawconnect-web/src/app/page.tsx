import Link from "next/link";
import { HeroActions } from "@/app/hero-actions";
import { PetCard } from "@/components/pets/pet-card";
import { buttonClasses } from "@/components/ui/button";
import { getLatestPets } from "@/lib/server/pets";

const steps = [
  { title: "Browse", text: "Search rescued pets near you and filter by species, size and age." },
  { title: "Apply", text: "Tell the rescuer about your home and how you plan to care for the pet." },
  { title: "Welcome home", text: "The rescuer reviews your application and arranges the adoption or foster." },
];

export default async function HomePage() {
  const latest = await getLatestPets(4);

  return (
    <>
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-16">
        <h1 className="max-w-2xl text-4xl font-bold tracking-tight text-stone-900 sm:text-5xl">
          Give a rescued pet a home, or a safe place to stay.
        </h1>
        <p className="max-w-xl text-lg text-stone-600">
          PawConnect connects independent rescuers and small shelters with people who want to adopt or foster.
        </p>
        <HeroActions />
      </section>

      {latest.length > 0 && (
        <section aria-labelledby="latest-heading" className="mx-auto w-full max-w-6xl px-4 pb-12">
          <div className="flex items-end justify-between gap-4">
            <h2 id="latest-heading" className="text-2xl font-bold text-stone-900">
              Recently listed
            </h2>
            <Link href="/pets" className={buttonClasses("secondary", "sm")}>
              See all pets
            </Link>
          </div>
          <ul className="mt-5 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {latest.map((pet) => (
              <PetCard key={pet.id} pet={pet} />
            ))}
          </ul>
        </section>
      )}

      <section aria-labelledby="how-heading" className="mx-auto w-full max-w-6xl px-4 pb-16">
        <h2 id="how-heading" className="text-2xl font-bold text-stone-900">
          How it works
        </h2>
        <ol className="mt-5 grid gap-4 sm:grid-cols-3">
          {steps.map((step, index) => (
            <li key={step.title} className="rounded-2xl border border-stone-200 bg-white p-5">
              <span className="flex h-8 w-8 items-center justify-center rounded-full bg-orange-100 text-sm font-bold text-orange-800">
                {index + 1}
              </span>
              <h3 className="mt-3 font-semibold text-stone-900">{step.title}</h3>
              <p className="mt-1 text-sm text-stone-600">{step.text}</p>
            </li>
          ))}
        </ol>
      </section>
    </>
  );
}