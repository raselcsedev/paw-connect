"use client";

import { useWatch, type Control, type FieldValues, type Path } from "react-hook-form";

export function CharCount<T extends FieldValues>({
  control,
  name,
  max,
}: {
  control: Control<T>;
  name: Path<T>;
  max: number;
}) {
  const value = useWatch({ control, name });
  const length = typeof value === "string" ? value.length : 0;

  return (
    <p className="text-right text-xs text-stone-500" aria-hidden="true">
      {length}/{max}
    </p>
  );
}