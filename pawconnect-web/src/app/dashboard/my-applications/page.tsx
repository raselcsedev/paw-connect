import type { Metadata } from "next";
import { MyApplicationsView } from "@/components/applications/my-applications-view";
import { parseApplicationFilters } from "@/lib/applications/filters";
import type { RawSearchParams } from "@/lib/pets/search";

export const metadata: Metadata = { title: "My applications" };

export default async function MyApplicationsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const filters = parseApplicationFilters(await searchParams);
  return <MyApplicationsView filters={filters} />;
}