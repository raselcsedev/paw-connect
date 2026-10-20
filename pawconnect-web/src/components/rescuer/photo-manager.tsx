"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { PetImage } from "@/components/pets/pet-image";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ErrorState } from "@/components/ui/states";
import {
  useDeletePhoto,
  usePetPhotos,
  useRefreshPhotos,
  useSetPrimaryPhoto,
  useUploadPhoto,
} from "@/hooks/use-photos";
import { ApiError } from "@/lib/api-client";
import { errorMessage } from "@/lib/errors";
import { photoSrc } from "@/lib/pets/format";
import type { PetPhotoDto } from "@/types/pets";

const MAX_PHOTOS = 8;
const MAX_BYTES = 5 * 1024 * 1024;
const ACCEPTED = ["image/jpeg", "image/png", "image/webp"];

// These checks only save a pointless upload. The server reads the real file and is the authority.
function checkFile(file: File): string | null {
  if (file.size === 0) return "the file is empty";
  if (file.size > MAX_BYTES) return "larger than 5 MB";
  if (file.type && !ACCEPTED.includes(file.type)) return "not a JPEG, PNG or WebP image";
  return null;
}

// Statuses that mean "stop, the remaining files would fail too".
const STOP_STATUSES = [0, 403, 404, 409, 429];

export function PhotoManager({ petId, petName }: { petId: number; petName: string }) {
  const photosQuery = usePetPhotos(petId);
  const upload = useUploadPhoto();
  const setPrimary = useSetPrimaryPhoto(petId);
  const remove = useDeletePhoto(petId);
  const refresh = useRefreshPhotos(petId);

  const inputRef = useRef<HTMLInputElement>(null);
  const [progress, setProgress] = useState<{ current: number; total: number } | null>(null);
  const [problems, setProblems] = useState<string[]>([]);
  const [toDelete, setToDelete] = useState<{ photo: PetPhotoDto; number: number } | null>(null);

  const photos = photosQuery.data ?? [];
  const slots = MAX_PHOTOS - photos.length;
  const uploading = progress !== null;
  const busy = uploading || setPrimary.isPending || remove.isPending;

  async function handleFiles(files: FileList | null) {
    if (!files || files.length === 0) return;

    const found: string[] = [];
    const valid: File[] = [];

    for (const file of Array.from(files)) {
      const problem = checkFile(file);
      if (problem) {
        found.push(`${file.name}: ${problem}.`);
      } else {
        valid.push(file);
      }
    }

    const accepted = valid.slice(0, Math.max(0, slots));
    const skipped = valid.length - accepted.length;

    if (skipped > 0) {
      found.push(`${skipped} ${skipped === 1 ? "file was" : "files were"} skipped: a listing can have at most ${MAX_PHOTOS} photos.`);
    }

    setProblems([]);
    let uploaded = 0;

    for (let index = 0; index < accepted.length; index++) {
      setProgress({ current: index + 1, total: accepted.length });

      try {
        await upload.mutateAsync({ petId, file: accepted[index] });
        uploaded++;
      } catch (error) {
        found.push(`${accepted[index].name}: ${errorMessage(error)}`);
        if (error instanceof ApiError && STOP_STATUSES.includes(error.status)) break;
      }
    }

    setProgress(null);
    setProblems(found);

    if (uploaded > 0) {
      await refresh();
      toast.success(uploaded === 1 ? "Photo added." : `${uploaded} photos added.`);
    }
  }

  async function makeCover(photo: PetPhotoDto) {
    try {
      await setPrimary.mutateAsync({ petId, photoId: photo.id });
      toast.success("Cover photo updated.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  }

  async function confirmDelete() {
    if (!toDelete) return;

    try {
      await remove.mutateAsync({ petId, photoId: toDelete.photo.id });
      toast.success("Photo deleted.");
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setToDelete(null);
    }
  }

  return (
    <section aria-labelledby="photos-heading" className="rounded-2xl border border-stone-200 bg-white p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="photos-heading" className="text-base font-semibold text-stone-900">
            Photos
          </h2>
          <p className="mt-1 text-sm text-stone-600">
            {photosQuery.isSuccess ? `${photos.length} of ${MAX_PHOTOS} photos. ` : ""}
            JPEG, PNG or WebP, up to 5 MB each. Photos upload straight away, with no need to save the form.
          </p>
        </div>

        <div>
          <input
            ref={inputRef}
            type="file"
            multiple
            accept={ACCEPTED.join(",")}
            tabIndex={-1}
            className="sr-only"
            onChange={(event) => {
              const input = event.currentTarget;
              void handleFiles(input.files).finally(() => {
                input.value = "";
              });
            }}
          />
          <Button
            size="sm"
            disabled={busy || slots <= 0 || !photosQuery.isSuccess}
            loading={uploading}
            onClick={() => inputRef.current?.click()}
          >
            {slots <= 0 ? "Photo limit reached" : "Add photos"}
          </Button>
        </div>
      </div>

      <p className="mt-3 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-900">
        Photos from phones can carry hidden location data. We do not remove it yet, so only share photos that are safe to
        publish.
      </p>

      <div role="status" aria-live="polite" className="mt-3 text-sm text-stone-600">
        {progress ? `Uploading photo ${progress.current} of ${progress.total}…` : ""}
      </div>

      {problems.length > 0 && (
        <div role="alert" className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800">
          <p className="font-medium">Some photos were not added:</p>
          <ul className="mt-1 list-disc space-y-0.5 pl-5">
            {problems.map((problem) => (
              <li key={problem}>{problem}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="mt-4">
        {photosQuery.isError ? (
          <ErrorState message={photosQuery.error.message} onRetry={() => photosQuery.refetch()} />
        ) : photosQuery.isPending ? (
          <div role="status" aria-label="Loading photos" className="grid animate-pulse gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="aspect-[4/3] rounded-xl bg-stone-200" />
            ))}
          </div>
        ) : photos.length === 0 ? (
          <p className="rounded-xl border border-dashed border-stone-300 p-6 text-center text-sm text-stone-600">
            No photos yet. Listings with photos get far more attention.
          </p>
        ) : (
          <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {photos.map((photo, index) => {
              const number = index + 1;

              return (
                <li key={photo.id} className="overflow-hidden rounded-xl border border-stone-200">
                  <div className="relative aspect-[4/3] bg-stone-100">
                    <PetImage src={photoSrc(petId, photo.id)} alt={`${petName}, photo ${number}`} fallback="🖼️" />
                    {photo.isPrimary && (
                      <span className="absolute left-2 top-2">
                        <Badge tone="orange">Cover photo</Badge>
                      </span>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-2 p-2">
                    {!photo.isPrimary && (
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={busy}
                        aria-label={`Make photo ${number} the cover photo`}
                        onClick={() => makeCover(photo)}
                      >
                        Make cover
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={busy}
                      aria-label={`Delete photo ${number}`}
                      onClick={() => setToDelete({ photo, number })}
                    >
                      Delete
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      <ConfirmDialog
        open={toDelete !== null}
        title={`Delete photo ${toDelete?.number ?? ""}?`}
        message="The photo is removed from the listing and cannot be recovered."
        confirmLabel="Delete photo"
        cancelLabel="Keep it"
        loading={remove.isPending}
        onConfirm={confirmDelete}
        onCancel={() => setToDelete(null)}
      />
    </section>
  );
}