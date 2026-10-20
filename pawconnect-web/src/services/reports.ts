import { apiFetch } from "@/lib/api-client";
import type { ReportReason } from "@/types/admin";

export const submitReport = ({ petId, reason, details }: { petId: number; reason: ReportReason; details?: string }) =>
  apiFetch<{ id: number }>(`/pets/${petId}/reports`, { method: "POST", body: { reason, details } });