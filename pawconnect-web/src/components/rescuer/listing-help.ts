import { LISTING_HELP } from "@/lib/rescuer/format";
import type { ListingStatus } from "@/types/pets";

export function LISTING_HELP_FOR(status: ListingStatus): string {
  return LISTING_HELP[status];
}