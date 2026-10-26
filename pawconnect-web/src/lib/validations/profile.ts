import { z } from "zod";
import { passwordSchema } from "@/lib/validations/auth";

export const profileSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(2, "Enter at least 2 characters.")
    .max(100, "Keep it under 100 characters."),
});

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Enter your current password."),
    newPassword: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  })
  .refine((v) => v.newPassword !== v.currentPassword, {
    message: "Choose a password that is different from your current one.",
    path: ["newPassword"],
  });

export type ProfileFormValues = z.infer<typeof profileSchema>;
export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;