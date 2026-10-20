import type { Metadata } from "next";
import { ReportsView } from "@/components/admin/reports-view";
import { parseReportFilters } from "@/lib/admin/filters";
import type { RawSearchParams } from "@/lib/query-params";

export const metadata: Metadata = { title: "Reports", robots: { index: false } };

export default async function AdminReportsPage({ searchParams }: { searchParams: Promise<RawSearchParams> }) {
  return <ReportsView filters={parseReportFilters(await searchParams)} />;
}