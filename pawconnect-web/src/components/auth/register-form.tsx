"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCurrentUser, useRegister } from "@/hooks/use-auth";
import { applyServerErrors } from "@/lib/forms";
import { safeRedirect } from "@/lib/redirect";
import { registerSchema, type RegisterFormValues } from "@/lib/validations/auth";

const roles = [
  { value: "Adopter", title: "I want to adopt or foster", text: "Browse pets and send applications." },
  { value: "Rescuer", title: "I am a rescuer or shelter", text: "List pets and review applications." },
] as const;

export function RegisterForm({ next }: { next?: string }) {
  const router = useRouter();
  const destination = safeRedirect(next);
  const { data: user } = useCurrentUser();
  const registerUser = useRegister();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { displayName: "", email: "", password: "", confirmPassword: "", role: "Adopter" },
  });

  useEffect(() => {
    if (user) {
      router.replace(destination);
    }
  }, [user, destination, router]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await registerUser.mutateAsync({
        email: values.email.trim(),
        password: values.password,
        displayName: values.displayName,
        role: values.role,
      });
      toast.success("Your account is ready. Welcome to PawConnect!");
      router.replace(destination);
    } catch (error) {
      const message = applyServerErrors(error, setError, ["email", "password", "displayName", "role"]);
      if (message) {
        setError("root.serverError", { type: "server", message });
      }
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-bold text-stone-900">Create your account</h1>
      <p className="mt-1 text-sm text-stone-600">It takes a minute. You can adopt, foster, or list pets.</p>

      <form onSubmit={onSubmit} noValidate className="mt-6 space-y-4">
        {errors.root?.serverError && (
          <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
            {errors.root.serverError.message}
          </p>
        )}

        <fieldset>
          <legend className="mb-2 text-sm font-medium text-stone-800">I am joining as</legend>
          <div className="grid gap-3 sm:grid-cols-2">
            {roles.map((role) => (
              <label
                key={role.value}
                className="flex cursor-pointer gap-3 rounded-xl border border-stone-200 p-3 text-sm has-[:checked]:border-orange-600 has-[:checked]:bg-orange-50"
              >
                <input type="radio" value={role.value} className="mt-1 accent-orange-600" {...register("role")} />
                <span>
                  <span className="block font-medium text-stone-900">{role.title}</span>
                  <span className="text-stone-600">{role.text}</span>
                </span>
              </label>
            ))}
          </div>
          {errors.role && (
            <p role="alert" className="mt-1.5 text-sm text-red-600">
              {errors.role.message}
            </p>
          )}
        </fieldset>

        <FormField id="displayName" label="Your name" error={errors.displayName?.message}>
          <Input
            id="displayName"
            autoComplete="name"
            invalid={!!errors.displayName}
            aria-describedby={errors.displayName ? "displayName-error" : undefined}
            {...register("displayName")}
          />
        </FormField>

        <FormField id="email" label="Email" error={errors.email?.message}>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            invalid={!!errors.email}
            aria-describedby={errors.email ? "email-error" : undefined}
            {...register("email")}
          />
        </FormField>

        <FormField
          id="password"
          label="Password"
          error={errors.password?.message}
          hint="At least 10 characters, with an uppercase letter, a lowercase letter and a number."
        >
          <Input
            id="password"
            type="password"
            autoComplete="new-password"
            invalid={!!errors.password}
            aria-describedby={errors.password ? "password-error" : "password-hint"}
            {...register("password")}
          />
        </FormField>

        <FormField id="confirmPassword" label="Confirm password" error={errors.confirmPassword?.message}>
          <Input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            invalid={!!errors.confirmPassword}
            aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
            {...register("confirmPassword")}
          />
        </FormField>

        <Button type="submit" className="w-full" loading={registerUser.isPending}>
          Create account
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-stone-600">
        Already have an account?{" "}
        <Link href="/login" className="font-medium text-orange-700 hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
}