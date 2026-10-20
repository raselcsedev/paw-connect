import type { Metadata } from "next";
import Link from "next/link";
import { PetForm } from "@/components/rescuer/pet-form";

export const metadata: Metadata = { title: "Create a listing" };

export default function NewListingPage() {
  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="text-sm text-stone-600">
        <Link href="/dashboard/rescuer/pets" className="hover:underline">
          ← My listings
        </Link>
      </nav>

      <div>
        <h1 className="text-2xl font-bold text-stone-900">Create a listing</h1>
        <p className="mt-1 text-sm text-stone-600">
          New listings are saved as drafts. Only you can see a draft until you publish it.
        </p>
      </div>

      <PetForm />
    </div>
  );
}