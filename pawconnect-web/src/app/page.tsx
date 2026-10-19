import { HeroActions } from "@/app/hero-actions";

export default function HomePage() {
  return (
    <section className="mx-auto flex w-full max-w-6xl flex-1 flex-col justify-center gap-6 px-4 py-16">
      <h1 className="max-w-2xl text-4xl font-bold tracking-tight text-stone-900 sm:text-5xl">
        Give a rescued pet a home, or a safe place to stay.
      </h1>
      <p className="max-w-xl text-lg text-stone-600">
        PawConnect connects independent rescuers and small shelters with people who want to adopt or foster.
      </p>
      <HeroActions />
    </section>
  );
}