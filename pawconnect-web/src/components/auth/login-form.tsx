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
import { useCurrentUser, useLogin } from "@/hooks/use-auth";
import { applyServerErrors } from "@/lib/forms";
import { safeRedirect } from "@/lib/redirect";
import { loginSchema, type LoginFormValues } from "@/lib/validations/auth";

export function LoginForm({ next }: { next?: string }) {
  const router = useRouter();
  const destination = safeRedirect(next);
  const { data: user } = useCurrentUser();
  const login = useLogin();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  useEffect(() => {
    if (user) {
      router.replace(destination);
    }
  }, [user, destination, router]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await login.mutateAsync({ email: values.email.trim(), password: values.password });
      toast.success("Welcome back!");
      router.replace(destination);
    } catch (error) {
      const message = applyServerErrors(error, setError, ["email", "password"]);
      if (message) {
        setError("root.serverError", { type: "server", message });
      }
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-bold text-stone-900">Sign in</h1>
      <p className="mt-1 text-sm text-stone-600">Welcome back. Sign in to manage your applications or listings.</p>

      <form onSubmit={onSubmit} noValidate className="mt-6 space-y-4">
        {errors.root?.serverError && (
          <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
            {errors.root.serverError.message}
          </p>
        )}

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

        <FormField id="password" label="Password" error={errors.password?.message}>
          <Input
            id="password"
            type="password"
            autoComplete="current-password"
            invalid={!!errors.password}
            aria-describedby={errors.password ? "password-error" : undefined}
            {...register("password")}
          />
        </FormField>

        <Button type="submit" className="w-full" loading={login.isPending}>
          Sign in
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-stone-600">
        New to PawConnect?{" "}
        <Link href="/register" className="font-medium text-orange-700 hover:underline">
          Create an account
        </Link>
      </p>
    </div>
  );
}