export default function ApplyLoading() {
  return (
    <div role="status" aria-label="Loading form" className="mx-auto w-full max-w-6xl animate-pulse px-4 py-8">
      <div className="h-9 w-72 rounded bg-stone-200" />
      <div className="mt-6 grid gap-8 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-48 rounded-2xl bg-stone-200" />
          ))}
        </div>
        <div className="h-72 rounded-2xl bg-stone-200" />
      </div>
    </div>
  );
}