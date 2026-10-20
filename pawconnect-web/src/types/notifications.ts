export type NotificationType =
  | "ApplicationSubmitted"
  | "ApplicationStatusChanged"
  | "ApplicationWithdrawn"
  | "AdoptionCompleted"
  | "ListingModerated"
  | "ReportReviewed";

export interface NotificationDto {
  id: number;
  type: NotificationType;
  title: string;
  message: string;
  applicationId: number | null;
  petId: number | null;
  isRead: boolean;
  createdAtUtc: string;
}

export interface UnreadCountDto {
  count: number;
}