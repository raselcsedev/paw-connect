export function safeRedirect(target: string | null | undefined, fallback = "/dashboard"): string {
  if (!target) return fallback;
  if (!target.startsWith("/")) return fallback;
  if (target.startsWith("//") || target.startsWith("/\\")) return fallback;
  return target;
}