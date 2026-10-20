import type { Metadata } from "next";
import { ReportReview } from "@/components/admin/report-review";
import { parsePetId } from "@/lib/pets/id";

export const metadata: Metadata = { title: "Review report", robots: { index: false } };

export default async function ReviewReportPage({ params }: { params: Promise<{ id: string }> }) {
  return <ReportReview id={parsePetId((await params).id)} />;
}