export function PetGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div role="status" aria-label="Loading pets" className="mt-4 grid animate-pulse gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="overflow-hidden rounded-2xl border border-stone-200 bg-white">
          <div className="aspect-[4/3] bg-stone-200" />
          <div className="space-y-3 p-4">
            <div className="h-5 w-2/3 rounded bg-stone-200" />
            <div className="h-4 w-1/2 rounded bg-stone-200" />
            <div className="h-4 w-1/3 rounded bg-stone-200" />
          </div>
        </div>
      ))}
    </div>
  );
}