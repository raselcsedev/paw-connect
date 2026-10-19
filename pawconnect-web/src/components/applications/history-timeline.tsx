import { StatusBadge } from "@/components/applications/status-badge";
import { formatDateTime, STATUS_LABELS } from "@/lib/applications/format";
import type { StatusHistoryDto } from "@/types/applications";

function describe(entry: StatusHistoryDto): string {
  if (entry.fromStatus === null) return "Application submitted";
  if (entry.fromStatus === entry.toStatus) return "Update";
  return `Status changed from ${STATUS_LABELS[entry.fromStatus]} to ${STATUS_LABELS[entry.toStatus]}`;
}

export function HistoryTimeline({ history }: { history: StatusHistoryDto[] }) {
  const newestFirst = [...history].reverse();

  return (
    <ol className="space-y-4 border-l-2 border-stone-200 pl-5">
      {newestFirst.map((entry, index) => (
        <li key={`${entry.changedAtUtc}-${index}`} className="relative">
          <span aria-hidden="true" className="absolute -left-[1.62rem] top-1.5 h-3 w-3 rounded-full border-2 border-white bg-orange-500" />
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-sm font-medium text-stone-900">{describe(entry)}</p>
            <StatusBadge status={entry.toStatus} />
          </div>
          {entry.note && <p className="mt-1 text-sm text-stone-700">{entry.note}</p>}
          <p className="mt-1 text-xs text-stone-500">
            {formatDateTime(entry.changedAtUtc)} · {entry.changedBy}
          </p>
        </li>
      ))}
    </ol>
  );
}