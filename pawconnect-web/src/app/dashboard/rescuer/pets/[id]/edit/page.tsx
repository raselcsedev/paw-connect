import type { Metadata } from "next";
import { EditPetView } from "@/components/rescuer/edit-pet-view";
import { parsePetId } from "@/lib/pets/id";

export const metadata: Metadata = { title: "Edit listing", robots: { index: false } };

export default async function EditListingPage({ params }: { params: Promise<{ id: string }> }) {
  return <EditPetView id={parsePetId((await params).id)} />;
}