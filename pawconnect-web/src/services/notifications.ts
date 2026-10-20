import { apiFetch } from "@/lib/api-client";
import { notificationsApiQuery, type NotificationFilters } from "@/lib/notifications/filters";
import type { NotificationDto, UnreadCountDto } from "@/types/notifications";
import type { PagedResult } from "@/types/pets";

export const fetchNotifications = (filters: NotificationFilters) =>
  apiFetch<PagedResult<NotificationDto>>(`/notifications?${notificationsApiQuery(filters)}`);

export const fetchUnreadCount = () => apiFetch<UnreadCountDto>("/notifications/unread-count");

export const markNotificationRead = (id: number) =>
  apiFetch<void>(`/notifications/${id}/read`, { method: "PATCH" });

export const markAllNotificationsRead = () =>
  apiFetch<{ updated: number }>("/notifications/read-all", { method: "POST" });