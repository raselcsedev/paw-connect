import type { Metadata } from "next";
import { IncomingView } from "@/components/rescuer/incoming-view";
import type { RawSearchParams } from "@/lib/query-params";
import { parseIncomingFilters } from "@/lib/rescuer/application-filters";

export const metadata: Metadata = { title: "Applications" };

export default async function IncomingApplicationsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const filters = parseIncomingFilters(await searchParams);
  return <IncomingView filters={filters} />;
}