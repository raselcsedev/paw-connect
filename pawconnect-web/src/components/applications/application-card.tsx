import Link from "next/link";
import { StatusBadge } from "@/components/applications/status-badge";
import { WithdrawButton } from "@/components/applications/withdraw-button";
import { Badge } from "@/components/ui/badge";
import { buttonClasses } from "@/components/ui/button";
import { formatDateOnly, STATUS_HELP, TYPE_LABELS } from "@/lib/applications/format";
import { formatDate } from "@/lib/pets/format";
import type { ApplicationSummaryDto } from "@/types/applications";

export function ApplicationCard({ application }: { application: ApplicationSummaryDto }) {
  return (
    <li className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm sm:flex sm:items-center sm:justify-between sm:gap-4">
      <div className="min-w-0 space-y-1.5">
        <div className="flex flex-wrap items-center gap-2">
          <Link href={`/pets/${application.petId}`} className="text-base font-semibold text-stone-900 hover:underline">
            {application.petName}
          </Link>
          <Badge>{TYPE_LABELS[application.type]}</Badge>
          <StatusBadge status={application.status} />
        </div>
        <p className="text-sm text-stone-600">
          Submitted {formatDate(application.createdAtUtc)} · Updated {formatDate(application.updatedAtUtc)}
          {application.fosterStartDate && <> · Foster from {formatDateOnly(application.fosterStartDate)}</>}
        </p>
        <p className="text-sm text-stone-500">{STATUS_HELP[application.status]}</p>
      </div>

      <div className="mt-3 flex shrink-0 gap-2 sm:mt-0">
        <Link href={`/dashboard/my-applications/${application.id}`} className={buttonClasses("secondary", "sm")}>
          View details
        </Link>
        {application.status === "Pending" && <WithdrawButton id={application.id} petName={application.petName} />}
      </div>
    </li>
  );
}