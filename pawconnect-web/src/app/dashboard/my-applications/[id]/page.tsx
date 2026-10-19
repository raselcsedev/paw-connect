import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApplicationDetail } from "@/components/applications/application-detail";

export const metadata: Metadata = { title: "Application details", robots: { index: false } };

export default async function ApplicationPage({ params }: { params: Promise<{ id: string }> }) {
  const raw = (await params).id;

  if (!/^\d+$/.test(raw) || !Number.isSafeInteger(Number(raw)) || Number(raw) < 1) {
    notFound();
  }

  return <ApplicationDetail id={Number(raw)} />;
}