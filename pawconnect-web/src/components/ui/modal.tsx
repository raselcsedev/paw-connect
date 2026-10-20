"use client";

import { useEffect, useId, useRef } from "react";

export function Modal({
  open,
  title,
  onClose,
  children,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;

    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-lg rounded-2xl border border-stone-200 bg-white p-0 shadow-xl backdrop:bg-black/40"
    >
      <div className="p-6">
        <div className="flex items-start justify-between gap-4">
          <h2 id={titleId} className="text-lg font-semibold text-stone-900">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded-lg p-1 text-stone-500 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-orange-600"
          >
            <span aria-hidden="true">✕</span>
          </button>
        </div>
        {/* Children mount only while open, so every form starts fresh each time. */}
        <div className="mt-4">{open ? children : null}</div>
      </div>
    </dialog>
  );
}