"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { ErrorState, PageSkeleton } from "@/components/ui/states";
import { useCurrentUser } from "@/hooks/use-auth";
import type { Role } from "@/types/api";

export function RequireAuth({ roles, children }: { roles?: Role[]; children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { data: user, isPending, isError, error, refetch } = useCurrentUser();

  useEffect(() => {
    if (!isPending && !isError && user === null) {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [isPending, isError, user, pathname, router]);

  if (isPending || (!isError && !user)) {
    return <PageSkeleton />;
  }

  if (isError) {
    return (
      <div className="mx-auto w-full max-w-2xl px-4 py-12">
        <ErrorState message={error.message} onRetry={() => refetch()} />
      </div>
    );
  }

  if (user && roles && !roles.includes(user.role)) {
    return (
      <div className="mx-auto w-full max-w-2xl px-4 py-12">
        <ErrorState title="You do not have access to this page" message="This area is for a different type of account." />
      </div>
    );
  }

  return <>{children}</>;
}