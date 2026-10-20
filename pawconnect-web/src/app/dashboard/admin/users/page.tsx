import type { Metadata } from "next";
import { UsersView } from "@/components/admin/users-view";
import { parseUserFilters } from "@/lib/admin/filters";
import type { RawSearchParams } from "@/lib/query-params";

export const metadata: Metadata = { title: "Users", robots: { index: false } };

export default async function AdminUsersPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  return <UsersView filters={parseUserFilters(await searchParams)} />;
}