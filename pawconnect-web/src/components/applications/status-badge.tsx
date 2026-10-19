import { Badge } from "@/components/ui/badge";
import { STATUS_LABELS, STATUS_TONES } from "@/lib/applications/format";
import type { ApplicationStatus } from "@/types/applications";

export function StatusBadge({ status }: { status: ApplicationStatus }) {
  return <Badge tone={STATUS_TONES[status]}>{STATUS_LABELS[status]}</Badge>;
}