"use client";

import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";
import { Button, buttonClasses } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { useArchivePet, useChangeAvailability } from "@/hooks/use-rescuer";
import { errorMessage } from "@/lib/errors";
import type { PetDto } from "@/types/pets";
import type { SettableStatus } from "@/types/rescuer";

export function ListingRowActions({ pet }: { pet: PetDto }) {
  const change = useChangeAvailability();
  const archive = useArchivePet();
  const [confirming, setConfirming] = useState(false);

  const editable = pet.status !== "Adopted" && pet.status !== "Archived";
  const canPublish =
    (pet.status === "Draft" || pet.status === "Unavailable") &&
    !pet.isSuspended;
  const canPause = pet.status === "Available";
  const publicPage =
    (pet.status === "Available" || pet.status === "Adopted") &&
    !pet.isSuspended;

  async function setStatus(status: SettableStatus, success: string) {
    try {
      await change.mutateAsync({ id: pet.id, status });
      toast.success(success);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  }

  async function confirmArchive() {
    try {
      await archive.mutateAsync(pet.id);
      toast.success(`${pet.name} was archived.`);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setConfirming(false);
    }
  }

  const busy = change.isPending || archive.isPending;

  return (
    <div className="flex flex-wrap gap-2">
      {editable && (
        <Link
          href={`/dashboard/rescuer/pets/${pet.id}/edit`}
          className={buttonClasses("secondary", "sm")}
        >
          Edit
        </Link>
      )}
      {pet.status !== "Draft" && (
        <Link
          href={`/dashboard/rescuer/applications?petId=${pet.id}`}
          className={buttonClasses("ghost", "sm")}
        >
          Applications
        </Link>
      )}

      {canPublish && (
        <Button
          size="sm"
          disabled={busy}
          loading={change.isPending}
          onClick={() => setStatus("Available", `${pet.name} is now live.`)}
        >
          {pet.status === "Draft" ? "Publish" : "Make available"}
        </Button>
      )}

      {canPause && (
        <Button
          variant="secondary"
          size="sm"
          disabled={busy}
          loading={change.isPending}
          onClick={() =>
            setStatus(
              "Unavailable",
              `${pet.name} is paused and hidden from browsing.`,
            )
          }
        >
          Pause
        </Button>
      )}

      {publicPage && (
        <Link href={`/pets/${pet.id}`} className={buttonClasses("ghost", "sm")}>
          View
        </Link>
      )}

      {editable && (
        <>
          <Button
            variant="ghost"
            size="sm"
            disabled={busy}
            onClick={() => setConfirming(true)}
          >
            Archive
          </Button>
          <ConfirmDialog
            open={confirming}
            title={`Archive ${pet.name}?`}
            message={`${pet.name} will be hidden from everyone and you will not be able to edit it. Applications for this pet stay as they are, so review them before you archive.`}
            confirmLabel="Archive listing"
            loading={archive.isPending}
            onConfirm={confirmArchive}
            onCancel={() => setConfirming(false)}
          />
        </>
      )}
    </div>
  );
}
