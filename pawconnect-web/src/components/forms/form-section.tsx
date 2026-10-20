export function FormSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <fieldset className="space-y-4 rounded-2xl border border-stone-200 bg-white p-5">
      <legend className="px-1 text-base font-semibold text-stone-900">{title}</legend>
      {children}
    </fieldset>
  );
}