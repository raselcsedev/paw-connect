import { cn } from "@/lib/cn";

export function Select({ className, ...props }: React.ComponentProps<"select">) {
  return (
    <select
      className={cn(
        "block w-full rounded-lg border border-stone-300 bg-white px-3 py-2.5 text-sm text-stone-900",
        "focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-orange-600",
        className,
      )}
      {...props}
    />
  );
}