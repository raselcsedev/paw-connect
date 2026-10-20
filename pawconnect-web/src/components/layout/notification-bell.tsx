"use client";

import Link from "next/link";
import { useUnreadCount } from "@/hooks/use-notifications";

export function NotificationBell() {
  const { data } = useUnreadCount();
  const count = data?.count ?? 0;

  return (
    <Link
      href="/dashboard/notifications"
      aria-label={count > 0 ? `Notifications, ${count} unread` : "Notifications"}
      className="relative rounded-lg p-2 text-stone-700 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-orange-600"
    >
      <svg className="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M15 17h5l-1.4-1.4a2 2 0 01-.6-1.4V11a6 6 0 10-12 0v3.2c0 .5-.2 1-.6 1.4L4 17h5m6 0a3 3 0 11-6 0m6 0H9"
        />
      </svg>
      {count > 0 && (
        <span
          aria-hidden="true"
          className="absolute -right-0.5 -top-0.5 min-w-5 rounded-full bg-orange-600 px-1 text-center text-xs font-bold leading-5 text-white"
        >
          {count > 9 ? "9+" : count}
        </span>
      )}
    </Link>
  );
}