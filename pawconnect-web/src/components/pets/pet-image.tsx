"use client";

import { useState } from "react";
import { cn } from "@/lib/cn";

type PetImageProps = {
  src: string | null;
  alt: string;
  fallback: string;
  className?: string;
  eager?: boolean;
};

export function PetImage({ src, alt, fallback, className, eager = false }: PetImageProps) {
  const [failed, setFailed] = useState(false);

  if (!src || failed) {
    return (
      <div
        role="img"
        aria-label={alt}
        className={cn("flex h-full w-full items-center justify-center bg-orange-50 text-5xl", className)}
      >
        <span aria-hidden="true">{fallback}</span>
      </div>
    );
  }

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={src}
      alt={alt}
      loading={eager ? "eager" : "lazy"}
      decoding="async"
      onError={() => setFailed(true)}
      className={cn("h-full w-full object-cover", className)}
    />
  );
}