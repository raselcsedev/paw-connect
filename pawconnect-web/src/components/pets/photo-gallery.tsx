"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";

type GalleryPhoto = { id: number; src: string };

export function PhotoGallery({
  photos,
  name,
  fallbackEmoji,
}: {
  photos: GalleryPhoto[];
  name: string;
  fallbackEmoji: string;
}) {
  const [index, setIndex] = useState(0);

  if (photos.length === 0) {
    return (
      <div
        role="img"
        aria-label={`No photos of ${name} yet`}
        className="flex aspect-[4/3] w-full flex-col items-center justify-center gap-2 rounded-2xl bg-orange-50 text-7xl"
      >
        <span aria-hidden="true">{fallbackEmoji}</span>
        <span className="text-sm text-stone-600">No photos yet</span>
      </div>
    );
  }

  const count = photos.length;
  const current = photos[Math.min(index, count - 1)];
  const step = (delta: number) => setIndex((value) => (value + delta + count) % count);

  return (
    <div>
      <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-stone-100">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          key={current.id}
          src={current.src}
          alt={`${name}, photo ${index + 1} of ${count}`}
          decoding="async"
          className="h-full w-full object-cover"
        />

        {count > 1 && (
          <>
            <button
              type="button"
              aria-label="Previous photo"
              onClick={() => step(-1)}
              className="absolute left-3 top-1/2 -translate-y-1/2 rounded-full bg-white/90 p-2 shadow hover:bg-white focus-visible:outline-2 focus-visible:outline-orange-600"
            >
              <span aria-hidden="true">‹</span>
            </button>
            <button
              type="button"
              aria-label="Next photo"
              onClick={() => step(1)}
              className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full bg-white/90 p-2 shadow hover:bg-white focus-visible:outline-2 focus-visible:outline-orange-600"
            >
              <span aria-hidden="true">›</span>
            </button>
          </>
        )}
      </div>

      {count > 1 && (
        <ul className="mt-3 flex gap-2 overflow-x-auto pb-1" aria-label="Photo thumbnails">
          {photos.map((photo, i) => (
            <li key={photo.id} className="shrink-0">
              <button
                type="button"
                aria-label={`Show photo ${i + 1} of ${count}`}
                aria-current={i === index ? "true" : undefined}
                onClick={() => setIndex(i)}
                className={cn(
                  "block h-16 w-20 overflow-hidden rounded-lg border-2 focus-visible:outline-2 focus-visible:outline-orange-600",
                  i === index ? "border-orange-600" : "border-transparent opacity-80 hover:opacity-100",
                )}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={photo.src} alt="" loading="lazy" decoding="async" className="h-full w-full object-cover" />
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}