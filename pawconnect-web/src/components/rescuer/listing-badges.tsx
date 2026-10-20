import { Badge } from "@/components/ui/badge";
import { LISTING_LABELS, LISTING_TONES } from "@/lib/rescuer/format";
import type { PetDto } from "@/types/pets";

export function ListingBadges({ pet }: { pet: Pick<PetDto, "status" | "isSuspended"> }) {
  return (
    <div className="flex flex-wrap gap-1.5">
      <Badge tone={LISTING_TONES[pet.status]}>{LISTING_LABELS[pet.status]}</Badge>
      {pet.isSuspended && <Badge tone="red">Suspended</Badge>}
    </div>
  );
}