import type { ApplicationStatus, ApplicationType } from "@/types/applications";

export type ReviewTarget = "UnderReview" | "Approved" | "Rejected";

type Reviewable = {
  status: ApplicationStatus;
  type: ApplicationType;
  completedAtUtc: string | null;
};

/** Mirrors the API's state machine for the pet owner (Lesson 8.2). The API still enforces it. */
export function reviewActions(application: Reviewable): { targets: ReviewTarget[]; canComplete: boolean } {
  if (application.completedAtUtc) {
    return { targets: [], canComplete: false };
  }

  switch (application.status) {
    case "Pending":
      return { targets: ["UnderReview", "Approved", "Rejected"], canComplete: false };
    case "UnderReview":
      return { targets: ["Approved", "Rejected"], canComplete: false };
    case "Approved":
      return { targets: ["Rejected"], canComplete: application.type === "Adoption" };
    default:
      return { targets: [], canComplete: false };
  }
}

export function targetLabel(target: ReviewTarget, from: ApplicationStatus): string {
  if (target === "UnderReview") return "Start review";
  if (target === "Approved") return "Approve";
  return from === "Approved" ? "Revoke approval" : "Reject";
}

export function reviewHelp(application: Reviewable): string {
  if (application.completedAtUtc) {
    return "The adoption is complete. This application is now closed.";
  }

  switch (application.status) {
    case "Pending":
      return "This application is waiting for your decision.";
    case "UnderReview":
      return "You are reviewing this application. Approve or reject it when you have decided.";
    case "Approved":
      return application.type === "Adoption"
        ? "You approved this adoption. Complete the adoption once the pet has gone home, or reject it if things change."
        : "You approved this foster application. The pet stays listed until you decide otherwise.";
    case "Rejected":
      return "You rejected this application. It cannot be reopened.";
    case "Withdrawn":
      return "The applicant withdrew this application.";
  }
}