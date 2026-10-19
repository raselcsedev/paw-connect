import { notFound } from "next/navigation";

export function parsePetId(value: string): number {
  if (!/^\d+$/.test(value)) notFound();

  const id = Number(value);
  if (!Number.isSafeInteger(id) || id < 1) notFound();

  return id;
}