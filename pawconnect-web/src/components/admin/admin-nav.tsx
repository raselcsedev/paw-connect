"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAdminDashboard } from "@/hooks/use-admin";
import { cn } from "@/lib/cn";

const tabs = [
  { href: "/dashboard/admin", label: "Overview", exact: true },
  { href: "/dashboard/admin/reports", label: "Reports", exact: false },
  { href: "/dashboard/admin/pets", label: "Listings", exact: false },
  { href: "/dashboard/admin/users", label: "Users", exact: false },
];

export function AdminNav() {
  const pathname = usePathname();
  const { data } = useAdminDashboard();
  const open = data?.openReports ?? 0;

  return (
    <nav aria-label="Admin sections" className="mb-6 flex gap-1 overflow-x-auto border-b border-stone-200">
      {tabs.map((tab) => {
        const active = tab.exact ? pathname === tab.href : pathname === tab.href || pathname.startsWith(`${tab.href}/`);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={active ? "page" : undefined}
            className={cn(
              "-mb-px flex items-center gap-1.5 whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium",
              active ? "border-orange-600 text-orange-700" : "border-transparent text-stone-600 hover:text-stone-900",
            )}
          >
            {tab.label}
            {tab.label === "Reports" && open > 0 && (
              <span className="rounded-full bg-orange-600 px-1.5 text-xs font-bold text-white">
                {open}
                <span className="sr-only"> open</span>
              </span>
            )}
          </Link>
        );
      })}
    </nav>
  );
}