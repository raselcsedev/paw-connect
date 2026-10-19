"use client";

import { useRouter } from "next/navigation";
import { startTransition } from "react";
import { ErrorState } from "@/components/ui/states";

export default function PetsError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const router = useRouter();

  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-16">
      <ErrorState
        title="We could not load the pets"
        message="The pet service may be temporarily unavailable. Please try again in a moment."
        onRetry={() =>
          startTransition(() => {
            router.refresh();
            reset();
          })
        }
      />
    </div>
  );
}