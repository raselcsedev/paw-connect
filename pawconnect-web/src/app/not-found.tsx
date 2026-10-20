import Link from "next/link";
import { buttonClasses } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";

export default function NotFound() {
  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-16">
      <EmptyState
        title="We could not find that page"
        message="The link may be wrong, or the page may have moved."
        action={
          <Link href="/" className={buttonClasses("primary")}>
            Go to the home page
          </Link>
        }
      />
    </div>
  );
}