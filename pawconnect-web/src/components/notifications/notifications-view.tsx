"use client";

import Link from "next/link";
import { toast } from "sonner";
import { Pagination } from "@/components/pets/pagination";
import { Button, buttonClasses } from "@/components/ui/button";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useMarkAllRead, useMarkRead, useNotifications, useUnreadCount } from "@/hooks/use-notifications";
import { formatDateTime } from "@/lib/applications/format";
import { cn } from "@/lib/cn";
import { notificationHref, notificationsHref, type NotificationFilters } from "@/lib/notifications/filters";
import type { NotificationDto } from "@/types/notifications";

function NotificationRow({ notification }: { notification: NotificationDto }) {
  const markRead = useMarkRead();
  const href = notificationHref(notification);

  const body = (
    <>
      <p className="flex items-center gap-2 text-sm font-semibold text-stone-900">
        {!notification.isRead && (
          <>
            <span aria-hidden="true" className="h-2 w-2 shrink-0 rounded-full bg-orange-600" />
            <span className="sr-only">Unread: </span>
          </>
        )}
        {notification.title}
      </p>
      <p className="mt-1 text-sm text-stone-700">{notification.message}</p>
      <p className="mt-1 text-xs text-stone-500">{formatDateTime(notification.createdAtUtc)}</p>
    </>
  );

  return (
    <li
      className={cn(
        "flex items-start gap-3 rounded-2xl border p-4",
        notification.isRead ? "border-stone-200 bg-white" : "border-orange-200 bg-orange-50",
      )}
    >
      {href ? (
        <Link
          href={href}
          onClick={() => {
            if (!notification.isRead) markRead.mutate(notification.id);
          }}
          className="min-w-0 flex-1 rounded-lg focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange-600"
        >
          {body}
        </Link>
      ) : (
        <div className="min-w-0 flex-1">{body}</div>
      )}

      {!notification.isRead && (
        <Button
          size="sm"
          variant="ghost"
          loading={markRead.isPending}
          aria-label={`Mark "${notification.title}" as read`}
          onClick={() => markRead.mutate(notification.id)}
        >
          Mark read
        </Button>
      )}
    </li>
  );
}

export function NotificationsView({ filters }: { filters: NotificationFilters }) {
  const list = useNotifications(filters);
  const unread = useUnreadCount();
  const markAll = useMarkAllRead();

  async function readAll() {
    const result = await markAll.mutateAsync();
    toast.success(result.updated === 1 ? "1 notification marked as read." : `${result.updated} notifications marked as read.`);
  }

  const tab = (active: boolean) =>
    cn(
      "rounded-lg px-3 py-1.5 text-sm font-medium",
      active ? "bg-orange-100 text-orange-800" : "text-stone-700 hover:bg-stone-100",
    );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-stone-900">Notifications</h1>
          <p className="mt-1 text-sm text-stone-600">Updates about your applications, listings and reports.</p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          loading={markAll.isPending}
          disabled={(unread.data?.count ?? 0) === 0}
          onClick={() => void readAll().catch(() => undefined)}
        >
          Mark all as read
        </Button>
      </div>

      <nav aria-label="Notification filter" className="flex gap-2">
        <Link href={notificationsHref(filters, { unreadOnly: false })} aria-current={!filters.unreadOnly ? "page" : undefined} className={tab(!filters.unreadOnly)}>
          All
        </Link>
        <Link href={notificationsHref(filters, { unreadOnly: true })} aria-current={filters.unreadOnly ? "page" : undefined} className={tab(filters.unreadOnly)}>
          Unread
        </Link>
      </nav>

      {list.isError ? (
        <ErrorState message={list.error.message} onRetry={() => list.refetch()} />
      ) : list.isPending ? (
        <div role="status" aria-label="Loading notifications" className="animate-pulse space-y-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-20 rounded-2xl bg-stone-200" />
          ))}
        </div>
      ) : list.data.totalCount === 0 ? (
        <EmptyState
          title={filters.unreadOnly ? "You are all caught up" : "No notifications yet"}
          message={filters.unreadOnly ? "There is nothing unread." : "Updates will appear here as things happen."}
          action={
            filters.unreadOnly ? (
              <Link href="/dashboard/notifications" className={buttonClasses("secondary")}>
                Show all notifications
              </Link>
            ) : undefined
          }
        />
      ) : list.data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          action={
            <Link href={notificationsHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={list.isPlaceholderData}>
          <ul className="space-y-3">
            {list.data.items.map((notification) => (
              <NotificationRow key={notification.id} notification={notification} />
            ))}
          </ul>
          <Pagination
            page={list.data.page}
            totalPages={list.data.totalPages}
            hrefFor={(target) => notificationsHref(filters, { page: target })}
          />
        </section>
      )}
    </div>
  );
}