"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Button, type ButtonSize } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { useWithdrawApplication } from "@/hooks/use-applications";
import { ApiError } from "@/lib/api-client";

export function WithdrawButton({ id, petName, size = "sm" }: { id: number; petName: string; size?: ButtonSize }) {
  const [open, setOpen] = useState(false);
  const withdraw = useWithdrawApplication();

  async function confirm() {
    try {
      await withdraw.mutateAsync(id);
      toast.success(`Your application for ${petName} was withdrawn.`);
    } catch (error) {
      toast.error(
        error instanceof ApiError && error.status === 409
          ? "This application can no longer be withdrawn. It may have just been updated, so we refreshed it."
          : error instanceof ApiError
            ? error.message
            : "We could not withdraw the application. Please try again.",
      );
    } finally {
      setOpen(false);
    }
  }

  return (
    <>
      <Button variant="secondary" size={size} onClick={() => setOpen(true)}>
        Withdraw
      </Button>
      <ConfirmDialog
        open={open}
        title="Withdraw this application?"
        message={`The rescuer will be told that you withdrew your application for ${petName}. You can apply again later.`}
        confirmLabel="Yes, withdraw"
        loading={withdraw.isPending}
        onConfirm={confirm}
        onCancel={() => setOpen(false)}
      />
    </>
  );
}