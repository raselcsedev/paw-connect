import { first, integer, toSearchParams, type RawSearchParams } from "@/lib/query-params";
import type { NotificationDto } from "@/types/notifications";

export const NOTIFICATIONS_PAGE_SIZE = 20;

export interface NotificationFilters {
  unreadOnly: boolean;
  page: number;
}

export function parseNotificationFilters(raw: RawSearchParams): NotificationFilters {
  return {
    unreadOnly: first(raw.unread) === "1",
    page: integer(first(raw.page), 1, 100000) ?? 1,
  };
}

function params(f: NotificationFilters): URLSearchParams {
  return toSearchParams({ unread: f.unreadOnly ? "1" : undefined, page: f.page });
}

export function notificationsHref(f: NotificationFilters, o: Partial<NotificationFilters> = {}): string {
  const query = params({ ...f, page: 1, ...o }).toString();
  return query ? `/dashboard/notifications?${query}` : "/dashboard/notifications";
}

export function notificationsApiQuery(f: NotificationFilters): string {
  const search = new URLSearchParams();
  if (f.unreadOnly) search.set("unreadOnly", "true");
  search.set("page", String(f.page));
  search.set("pageSize", String(NOTIFICATIONS_PAGE_SIZE));
  return search.toString();
}

/** Where a notification leads. The type tells us who received it, so we know which area to open. */
export function notificationHref(n: NotificationDto): string | null {
  switch (n.type) {
    case "ApplicationSubmitted":
    case "ApplicationWithdrawn":
      return n.applicationId ? `/dashboard/rescuer/applications/${n.applicationId}` : null;
    case "ApplicationStatusChanged":
    case "AdoptionCompleted":
      return n.applicationId ? `/dashboard/my-applications/${n.applicationId}` : null;
    case "ListingModerated":
      return n.petId ? `/dashboard/rescuer/pets/${n.petId}/edit` : null;
    default:
      return null;
  }
}