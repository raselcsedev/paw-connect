export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center px-4 py-12">
      <div className="rounded-2xl border border-stone-200 bg-white p-6 shadow-sm sm:p-8">{children}</div>
    </div>
  );
}