"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { PageSkeleton } from "@/components/ui/states";
import { useCurrentUser } from "@/hooks/use-auth";

const homes = {
  Adopter: "/dashboard/my-applications",
  Rescuer: "/dashboard/rescuer",
  Admin: "/dashboard/admin",
} as const;

export default function DashboardPage() {
  const router = useRouter();
  const { data: user } = useCurrentUser();
  const home = user ? homes[user.role] : undefined;

  useEffect(() => {
    if (home) router.replace(home);
  }, [home, router]);

  return <PageSkeleton />;
}