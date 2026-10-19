import Link from "next/link";
import { cn } from "@/lib/cn";

function pageWindow(current: number, total: number): (number | "gap")[] {
  const wanted = new Set([1, total, current - 1, current, current + 1]);
  const pages = [...wanted].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | "gap")[] = [];

  pages.forEach((page, index) => {
    if (index > 0 && page - pages[index - 1] > 1) result.push("gap");
    result.push(page);
  });

  return result;
}

const itemClass =
  "inline-flex h-10 min-w-10 items-center justify-center rounded-lg border px-3 text-sm font-medium";

export function Pagination({
  page,
  totalPages,
  hrefFor,
}: {
  page: number;
  totalPages: number;
  hrefFor: (page: number) => string;
}) {
  if (totalPages <= 1) return null;

  return (
    <nav aria-label="Pagination" className="mt-8 flex flex-wrap items-center justify-center gap-2">
      {page > 1 ? (
        <Link href={hrefFor(page - 1)} rel="prev" className={cn(itemClass, "border-stone-300 bg-white hover:bg-stone-100")}>
          Previous
        </Link>
      ) : (
        <span aria-disabled="true" className={cn(itemClass, "border-stone-200 text-stone-400")}>
          Previous
        </span>
      )}

      {pageWindow(page, totalPages).map((entry, index) =>
        entry === "gap" ? (
          <span key={`gap-${index}`} aria-hidden="true" className="px-1 text-stone-500">
            …
          </span>
        ) : entry === page ? (
          <span key={entry} aria-current="page" className={cn(itemClass, "border-orange-600 bg-orange-600 text-white")}>
            {entry}
          </span>
        ) : (
          <Link
            key={entry}
            href={hrefFor(entry)}
            aria-label={`Page ${entry}`}
            className={cn(itemClass, "border-stone-300 bg-white hover:bg-stone-100")}
          >
            {entry}
          </Link>
        ),
      )}

      {page < totalPages ? (
        <Link href={hrefFor(page + 1)} rel="next" className={cn(itemClass, "border-stone-300 bg-white hover:bg-stone-100")}>
          Next
        </Link>
      ) : (
        <span aria-disabled="true" className={cn(itemClass, "border-stone-200 text-stone-400")}>
          Next
        </span>
      )}
    </nav>
  );
}