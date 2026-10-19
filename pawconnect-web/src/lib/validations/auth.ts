import { z } from "zod";

export const loginSchema = z.object({
  email: z.email("Enter a valid email address."),
  password: z.string().min(1, "Enter your password."),
});

export const registerSchema = z
  .object({
    displayName: z
      .string()
      .trim()
      .min(2, "Enter at least 2 characters.")
      .max(100, "Keep it under 100 characters."),
    email: z.email("Enter a valid email address."),
    password: z
      .string()
      .min(10, "Use at least 10 characters.")
      .regex(/[a-z]/, "Add a lowercase letter.")
      .regex(/[A-Z]/, "Add an uppercase letter.")
      .regex(/\d/, "Add a number."),
    confirmPassword: z.string(),
    role: z.enum(["Adopter", "Rescuer"], { error: "Choose how you will use PawConnect." }),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });

export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;