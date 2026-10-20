import type { RawSearchParams } from "@/lib/pets/search";

export type { RawSearchParams };

export function first(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

export function pick<T extends string>(value: string | undefined, allowed: readonly T[]): T | undefined {
  return allowed.find((item) => item === value);
}

export function integer(value: string | undefined, min: number, max: number): number | undefined {
  if (value === undefined || !/^\d+$/.test(value)) return undefined;
  const number = Number(value);
  return number >= min && number <= max ? number : undefined;
}