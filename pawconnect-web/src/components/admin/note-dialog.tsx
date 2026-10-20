"use client";

import { useId, useState } from "react";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import { Textarea } from "@/components/ui/textarea";
import { noteSchema } from "@/lib/validations/moderation";

type Props = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  confirmVariant?: "danger" | "primary";
  loading?: boolean;
  onConfirm: (note: string) => void;
  onCancel: () => void;
};

function NoteForm({ message, confirmLabel, confirmVariant = "danger", loading, onConfirm, onCancel }: Omit<Props, "open" | "title">) {
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const id = useId();

  function submit(event: React.FormEvent) {
    event.preventDefault();
    const result = noteSchema.safeParse(note);

    if (!result.success) {
      setError(result.error.issues[0]?.message ?? "Write a note.");
      return;
    }

    onConfirm(result.data);
  }

  return (
    <form onSubmit={submit} noValidate>
      <p className="text-sm text-stone-600">{message}</p>

      <label htmlFor={id} className="mt-4 block text-sm font-medium text-stone-800">
        Note (the owner can read this)
      </label>
      <Textarea
        id={id}
        rows={3}
        maxLength={300}
        value={note}
        invalid={!!error}
        aria-describedby={error ? `${id}-error` : undefined}
        onChange={(event) => {
          setNote(event.target.value);
          if (error) setError(null);
        }}
        className="mt-1.5"
      />
      <p aria-hidden="true" className="mt-1 text-right text-xs text-stone-500">
        {note.length}/300
      </p>
      {error && (
        <p id={`${id}-error`} role="alert" className="mt-1 text-sm text-red-600">
          {error}
        </p>
      )}

      <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button variant="secondary" onClick={onCancel} disabled={loading}>
          Cancel
        </Button>
        <Button type="submit" variant={confirmVariant} loading={loading}>
          {confirmLabel}
        </Button>
      </div>
    </form>
  );
}

export function NoteDialog({ open, title, loading, onCancel, ...rest }: Props) {
  return (
    <Modal open={open} title={title} onClose={loading ? () => undefined : onCancel}>
      <NoteForm {...rest} loading={loading} onCancel={onCancel} />
    </Modal>
  );
}