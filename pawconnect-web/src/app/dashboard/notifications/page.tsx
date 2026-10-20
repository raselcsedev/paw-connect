import type { Metadata } from "next";
import { NotificationsView } from "@/components/notifications/notifications-view";
import { parseNotificationFilters } from "@/lib/notifications/filters";
import type { RawSearchParams } from "@/lib/query-params";

export const metadata: Metadata = { title: "Notifications", robots: { index: false } };

export default async function NotificationsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  return <NotificationsView filters={parseNotificationFilters(await searchParams)} />;
}