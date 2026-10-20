import type { Metadata } from "next";
import { ApplicationReview } from "@/components/rescuer/application-review";
import { parsePetId } from "@/lib/pets/id";

export const metadata: Metadata = { title: "Review application", robots: { index: false } };

export default async function ReviewPage({ params }: { params: Promise<{ id: string }> }) {
  // Application ids use the same rule as pet ids: a plain positive integer.
  return <ApplicationReview id={parsePetId((await params).id)} />;
}