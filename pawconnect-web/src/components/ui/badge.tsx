import { cn } from "@/lib/cn";

const tones = {
  neutral: "bg-stone-100 text-stone-700",
  orange: "bg-orange-100 text-orange-800",
  green: "bg-emerald-100 text-emerald-800",
  red: "bg-red-100 text-red-800",
} as const;

export function Badge({
  tone = "neutral",
  children,
}: {
  tone?: keyof typeof tones;
  children: React.ReactNode;
}) {
  return (
    <span className={cn("inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium", tones[tone])}>
      {children}
    </span>
  );
}