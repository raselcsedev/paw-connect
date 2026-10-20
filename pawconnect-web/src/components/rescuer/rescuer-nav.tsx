"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";

const tabs = [
  { href: "/dashboard/rescuer", label: "Overview", exact: true },
  { href: "/dashboard/rescuer/pets", label: "My listings", exact: false },
];

export function RescuerNav() {
  const pathname = usePathname();

  return (
    <nav aria-label="Rescuer sections" className="mb-6 flex gap-1 overflow-x-auto border-b border-stone-200">
      {tabs.map((tab) => {
        const active = tab.exact ? pathname === tab.href : pathname === tab.href || pathname.startsWith(`${tab.href}/`);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={active ? "page" : undefined}
            className={cn(
              "-mb-px whitespace-nowrap border-b-2 px-4 py-2.5 text-sm font-medium",
              active ? "border-orange-600 text-orange-700" : "border-transparent text-stone-600 hover:text-stone-900",
            )}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}