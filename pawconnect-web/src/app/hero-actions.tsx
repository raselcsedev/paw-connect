"use client";

import Link from "next/link";
import { buttonClasses } from "@/components/ui/button";
import { useCurrentUser } from "@/hooks/use-auth";

export function HeroActions() {
  const { data: user, isPending } = useCurrentUser();

  if (isPending) {
    return <div className="h-12 w-56 animate-pulse rounded-lg bg-stone-200" aria-hidden="true" />;
  }

  if (user) {
    return (
      <Link href="/dashboard" className={buttonClasses("primary", "lg")}>
        Go to your dashboard
      </Link>
    );
  }

  return (
    <div className="flex flex-wrap gap-3">
      <Link href="/register" className={buttonClasses("primary", "lg")}>
        Create an account
      </Link>
      <Link href="/login" className={buttonClasses("secondary", "lg")}>
        Sign in
      </Link>
    </div>
  );
}