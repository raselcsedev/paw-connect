import type { Metadata } from "next";
import { ListingsView } from "@/components/rescuer/listings-view";
import type { RawSearchParams } from "@/lib/query-params";
import { parseListingFilters } from "@/lib/rescuer/filters";

export const metadata: Metadata = { title: "My listings" };

export default async function MyListingsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  const filters = parseListingFilters(await searchParams);
  return <ListingsView filters={filters} />;
}