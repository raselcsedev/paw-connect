import { Button } from "@/components/ui/button";

export function PageSkeleton() {
  return (
    <div role="status" aria-label="Loading" className="mx-auto w-full max-w-6xl animate-pulse space-y-6 px-4 py-8">
      <div className="h-8 w-64 rounded bg-stone-200" />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="h-28 rounded-xl bg-stone-200" />
        ))}
      </div>
    </div>
  );
}

export function ErrorState({
  title = "Something went wrong",
  message,
  onRetry,
}: {
  title?: string;
  message?: string;
  onRetry?: () => void;
}) {
  return (
    <div role="alert" className="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
      <h2 className="text-base font-semibold text-red-800">{title}</h2>
      {message && <p className="mt-1 text-sm text-red-700">{message}</p>}
      {onRetry && (
        <Button variant="secondary" size="sm" className="mt-4" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  );
}

export function EmptyState({
  title,
  message,
  action,
}: {
  title: string;
  message?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-dashed border-stone-300 bg-white p-10 text-center">
      <h2 className="text-base font-semibold text-stone-800">{title}</h2>
      {message && <p className="mt-1 text-sm text-stone-600">{message}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}