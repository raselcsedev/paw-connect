"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageSkeleton } from "@/components/ui/states";
import { useChangePassword, useUpdateProfile } from "@/hooks/use-account";
import { useCurrentUser } from "@/hooks/use-auth";
import { ApiError } from "@/lib/api-client";
import { applyServerErrors } from "@/lib/forms";
import {
  changePasswordSchema,
  profileSchema,
  type ChangePasswordFormValues,
  type ProfileFormValues,
} from "@/lib/validations/profile";
import type { UserDto } from "@/types/api";

function Card({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-stone-200 bg-white p-5 sm:p-6">
      <h2 className="text-lg font-semibold text-stone-900">{title}</h2>
      {description && <p className="mt-1 text-sm text-stone-600">{description}</p>}
      <div className="mt-4">{children}</div>
    </section>
  );
}

function ProfileForm({ user }: { user: UserDto }) {
  const update = useUpdateProfile();

  const {
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { displayName: user.displayName },
  });

  const unchanged = watch("displayName").trim() === user.displayName;

  const onSubmit = handleSubmit(async (values) => {
    try {
      const saved = await update.mutateAsync({ displayName: values.displayName });
      setValue("displayName", saved.displayName);
      toast.success("Your profile was updated.");
    } catch (error) {
      const message = applyServerErrors(error, setError, ["displayName"]);
      if (message) setError("root.serverError", { type: "server", message });
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {errors.root?.serverError && (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {errors.root.serverError.message}
        </p>
      )}

      <FormField
        id="displayName"
        label="Display name"
        error={errors.displayName?.message}
        hint="Rescuers and applicants see this name. It also appears in your status histories."
      >
        <Input
          id="displayName"
          autoComplete="name"
          maxLength={100}
          invalid={!!errors.displayName}
          aria-describedby={errors.displayName ? "displayName-error" : "displayName-hint"}
          {...register("displayName")}
        />
      </FormField>

      <Button type="submit" loading={update.isPending} disabled={unchanged}>
        Save changes
      </Button>
    </form>
  );
}

function PasswordForm({ onDone }: { onDone: () => void }) {
  const router = useRouter();
  const change = useChangePassword();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await change.mutateAsync({ currentPassword: values.currentPassword, newPassword: values.newPassword });
      toast.success("Your password was changed. Your other devices were signed out.");
      onDone();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        toast.error("Your session has expired. Please sign in again.");
        router.replace("/login?next=/dashboard/profile");
        return;
      }

      const message = applyServerErrors(error, setError, ["currentPassword", "newPassword"]);
      if (message) setError("root.serverError", { type: "server", message });
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-4">
      {errors.root?.serverError && (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
          {errors.root.serverError.message}
        </p>
      )}

      <FormField id="currentPassword" label="Current password" error={errors.currentPassword?.message}>
        <Input
          id="currentPassword"
          type="password"
          autoComplete="current-password"
          invalid={!!errors.currentPassword}
          aria-describedby={errors.currentPassword ? "currentPassword-error" : undefined}
          {...register("currentPassword")}
        />
      </FormField>

      <FormField
        id="newPassword"
        label="New password"
        error={errors.newPassword?.message}
        hint="At least 10 characters, with an uppercase letter, a lowercase letter and a number."
      >
        <Input
          id="newPassword"
          type="password"
          autoComplete="new-password"
          invalid={!!errors.newPassword}
          aria-describedby={errors.newPassword ? "newPassword-error" : "newPassword-hint"}
          {...register("newPassword")}
        />
      </FormField>

      <FormField id="confirmPassword" label="Confirm new password" error={errors.confirmPassword?.message}>
        <Input
          id="confirmPassword"
          type="password"
          autoComplete="new-password"
          invalid={!!errors.confirmPassword}
          aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
          {...register("confirmPassword")}
        />
      </FormField>

      <Button type="submit" loading={change.isPending}>
        Change password
      </Button>
    </form>
  );
}

export function ProfileView() {
  const { data: user } = useCurrentUser();
  const [passwordFormKey, setPasswordFormKey] = useState(0);

  if (!user) {
    return <PageSkeleton />;
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Your profile</h1>
        <p className="mt-1 text-sm text-stone-600">Manage how you appear on PawConnect and keep your account secure.</p>
      </div>

      <Card title="Account">
        <dl className="grid gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Email</dt>
            <dd className="mt-1 break-all text-sm text-stone-900">{user.email}</dd>
          </div>
          <div>
            <dt className="text-xs font-medium uppercase tracking-wide text-stone-500">Account type</dt>
            <dd className="mt-1">
              <Badge tone="orange">{user.role}</Badge>
            </dd>
          </div>
        </dl>
        <p className="mt-3 text-xs text-stone-500">
          Changing your email address is not available yet, because it needs email verification.
        </p>
      </Card>

      <Card title="Your details">
        <ProfileForm user={user} />
      </Card>

      <Card
        title="Change password"
        description="After you change your password, you stay signed in here and every other device is signed out."
      >
        <PasswordForm key={passwordFormKey} onDone={() => setPasswordFormKey((k) => k + 1)} />
      </Card>
    </div>
  );
}