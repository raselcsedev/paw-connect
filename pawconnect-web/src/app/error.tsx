"use client";

import { ErrorState } from "@/components/ui/states";

export default function RootError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-16">
      <ErrorState
        title="Something went wrong"
        message="An unexpected error occurred. Please try again."
        onRetry={reset}
      />
    </div>
  );
}