"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useCurrentUser } from "@/hooks/use-auth";
import { errorMessage } from "@/lib/errors";
import { notificationsApiQuery, type NotificationFilters } from "@/lib/notifications/filters";
import {
  fetchNotifications,
  fetchUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
} from "@/services/notifications";

export function useUnreadCount() {
  const { data: user } = useCurrentUser();

  return useQuery({
    queryKey: ["notifications", "unread"],
    queryFn: fetchUnreadCount,
    enabled: !!user,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });
}

export function useNotifications(filters: NotificationFilters) {
  return useQuery({
    queryKey: ["notifications", "list", notificationsApiQuery(filters)],
    queryFn: () => fetchNotifications(filters),
    placeholderData: keepPreviousData,
  });
}

function useRefreshNotifications() {
  const queryClient = useQueryClient();

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ["notifications"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
    ]);
}

export function useMarkRead() {
  const refresh = useRefreshNotifications();

  return useMutation({
    mutationFn: markNotificationRead,
    onError: (error) => toast.error(errorMessage(error)),
    onSettled: () => refresh(),
  });
}

export function useMarkAllRead() {
  const refresh = useRefreshNotifications();

  return useMutation({
    mutationFn: markAllNotificationsRead,
    onError: (error) => toast.error(errorMessage(error)),
    onSettled: () => refresh(),
  });
}