export function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl border border-stone-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-stone-600">{label}</p>
      <p className="mt-2 text-3xl font-bold tabular-nums text-stone-900">{value}</p>
    </div>
  );
}