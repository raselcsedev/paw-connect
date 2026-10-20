import type { Metadata } from "next";
import { AdminListingsView } from "@/components/admin/listings-view";
import { parseAdminPetFilters } from "@/lib/admin/filters";
import type { RawSearchParams } from "@/lib/query-params";

export const metadata: Metadata = { title: "Listings", robots: { index: false } };

export default async function AdminListingsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  return <AdminListingsView filters={parseAdminPetFilters(await searchParams)} />;
}