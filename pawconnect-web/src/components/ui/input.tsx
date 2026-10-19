import { cn } from "@/lib/cn";

type InputProps = React.ComponentProps<"input"> & { invalid?: boolean };

export function Input({ className, invalid, ...props }: InputProps) {
  return (
    <input
      aria-invalid={invalid || undefined}
      className={cn(
        "block w-full rounded-lg border bg-white px-3 py-2.5 text-sm text-stone-900 placeholder:text-stone-400",
        "focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-orange-600",
        invalid ? "border-red-500" : "border-stone-300",
        className,
      )}
      {...props}
    />
  );
}