import { z } from "zod";
import { REPORT_REASONS } from "@/lib/admin/filters";

export const noteSchema = z
  .string()
  .trim()
  .min(5, "Write at least 5 characters.")
  .max(300, "Please keep this under 300 characters.");

export const reviewSchema = z.object({
  decision: z.enum(["Resolved", "Dismissed"]),
  action: z.enum(["None", "Suspend", "Archive"]),
  note: noteSchema,
});

export type ReviewValues = z.infer<typeof reviewSchema>;

export const reportSchema = z
  .object({
    reason: z.enum(REPORT_REASONS, { error: "Choose a reason." }),
    details: z.string().trim().max(1000, "Please keep this under 1000 characters."),
  })
  .refine((v) => v.reason !== "Other" || v.details !== "", {
    message: "Please describe the problem.",
    path: ["details"],
  });

export type ReportValues = z.infer<typeof reportSchema>;