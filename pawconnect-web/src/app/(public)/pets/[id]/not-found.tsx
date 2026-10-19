import Link from "next/link";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";

export default function PetNotFound() {
  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-16">
      <EmptyState
        title="We could not find that pet"
        message="The listing may have been removed, or the link may be wrong."
        action={
          <Link href="/pets" className={buttonClasses("primary")}>
            Browse all pets
          </Link>
        }
      />
    </div>
  );
}