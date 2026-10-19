export default function PetDetailLoading() {
  return (
    <div role="status" aria-label="Loading pet" className="mx-auto w-full max-w-6xl animate-pulse px-4 py-8">
      <div className="mb-4 h-4 w-24 rounded bg-stone-200" />
      <div className="grid gap-8 lg:grid-cols-5">
        <div className="aspect-[4/3] rounded-2xl bg-stone-200 lg:col-span-3" />
        <div className="space-y-4 lg:col-span-2">
          <div className="h-9 w-2/3 rounded bg-stone-200" />
          <div className="h-5 w-1/2 rounded bg-stone-200" />
          <div className="h-12 rounded-lg bg-stone-200" />
          <div className="h-40 rounded-2xl bg-stone-200" />
        </div>
      </div>
    </div>
  );
}