export function BreakdownList({
  title,
  entries,
}: {
  title: string;
  entries: { label: string; value: number }[];
}) {
  const max = Math.max(1, ...entries.map((entry) => entry.value));

  return (
    <section className="rounded-2xl border border-stone-200 bg-white p-5">
      <h2 className="text-base font-semibold text-stone-900">{title}</h2>
      <ul className="mt-4 space-y-3">
        {entries.map((entry) => (
          <li key={entry.label}>
            <div className="flex items-center justify-between text-sm">
              <span className="text-stone-700">{entry.label}</span>
              <span className="font-medium tabular-nums text-stone-900">{entry.value}</span>
            </div>
            <div aria-hidden="true" className="mt-1 h-2 overflow-hidden rounded-full bg-stone-100">
              <div className="h-full rounded-full bg-orange-500" style={{ width: `${(entry.value / max) * 100}%` }} />
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}