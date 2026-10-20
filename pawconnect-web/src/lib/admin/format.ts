import type { AccountStatus, ReportReason, ReportStatus } from "@/types/admin";

export const REASON_LABELS: Record<ReportReason, string> = {
  Spam: "Spam",
  Inappropriate: "Inappropriate content",
  Misleading: "Misleading information",
  SuspectedScam: "Suspected scam",
  AnimalWelfare: "Animal welfare concern",
  Other: "Other",
};

export const REPORT_STATUS_LABELS: Record<ReportStatus, string> = {
  Open: "Open",
  Resolved: "Resolved",
  Dismissed: "Dismissed",
};

export const REPORT_STATUS_TONES: Record<ReportStatus, "orange" | "green" | "neutral"> = {
  Open: "orange",
  Resolved: "green",
  Dismissed: "neutral",
};

export const ACCOUNT_TONES: Record<AccountStatus, "green" | "red"> = {
  Active: "green",
  Disabled: "red",
};