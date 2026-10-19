"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useTransition } from "react";
import { FormField } from "@/components/forms/form-field";
import { buttonClasses } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { AVAILABILITY_FILTERS, SIZES, SORT_LABELS, SORTS, SPECIES } from "@/lib/pets/constants";
import { hasActiveFilters, type PetFilters } from "@/lib/pets/search";

const DEBOUNCE_MS = 400;

export function PetFilterBar({ filters }: { filters: PetFilters }) {
  const router = useRouter();
  const pathname = usePathname();
  const [pending, startTransition] = useTransition();

  const searchRef = useRef<HTMLInputElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined);

  // The URL is the source of truth. Build the next URL from what is in the address bar right now.
  function update(changes: Record<string, string | undefined>, mode: "push" | "replace" = "push") {
    const params = new URLSearchParams(window.location.search);

    for (const [key, value] of Object.entries(changes)) {
      if (value) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
    }

    params.delete("page");

    const query = params.toString();
    const url = query ? `${pathname}?${query}` : pathname;

    startTransition(() => {
      if (mode === "replace") {
        router.replace(url, { scroll: false });
      } else {
        router.push(url, { scroll: false });
      }
    });
  }

  function onSearchChange(value: string) {
    clearTimeout(timer.current);
    timer.current = setTimeout(() => update({ q: value.trim() || undefined }, "replace"), DEBOUNCE_MS);
  }

  useEffect(() => () => clearTimeout(timer.current), []);

  // Keep the box in sync when the URL changes from outside (Back button, a removed chip).
  useEffect(() => {
    const input = searchRef.current;
    if (input && document.activeElement !== input) {
      input.value = filters.q ?? "";
    }
  }, [filters.q]);

  function commitText(key: "city", raw: string, current?: string) {
    const value = raw.trim();
    if (value === (current ?? "")) return;
    update({ [key]: value || undefined });
  }

  function commitAge(key: "minAgeMonths" | "maxAgeMonths", raw: string, current?: number) {
    const value = raw.trim();

    if (value === "") {
      if (current !== undefined) update({ [key]: undefined });
      return;
    }

    if (!/^\d+$/.test(value)) return;

    const number = Number(value);
    if (number > 360 || number === current) return;

    update({ [key]: String(number) });
  }

  const blurOnEnter = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") event.currentTarget.blur();
  };

  return (
    <form
      role="search"
      aria-label="Filter pets"
      aria-busy={pending}
      onSubmit={(event) => event.preventDefault()}
      className="rounded-2xl border border-stone-200 bg-white p-4 shadow-sm"
    >
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="sm:col-span-2">
          <FormField id="filter-q" label="Search">
            <Input
              id="filter-q"
              ref={searchRef}
              type="search"
              placeholder="Name, breed or keyword"
              defaultValue={filters.q ?? ""}
              maxLength={100}
              onChange={(event) => onSearchChange(event.target.value)}
            />
          </FormField>
        </div>

        <FormField id="filter-species" label="Species">
          <Select
            id="filter-species"
            key={`species-${filters.species ?? ""}`}
            defaultValue={filters.species ?? ""}
            onChange={(event) => update({ species: event.target.value || undefined })}
          >
            <option value="">Any species</option>
            {SPECIES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="filter-size" label="Size">
          <Select
            id="filter-size"
            key={`size-${filters.size ?? ""}`}
            defaultValue={filters.size ?? ""}
            onChange={(event) => update({ size: event.target.value || undefined })}
          >
            <option value="">Any size</option>
            {SIZES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="filter-availability" label="Looking to">
          <Select
            id="filter-availability"
            key={`availability-${filters.availabilityType ?? ""}`}
            defaultValue={filters.availabilityType ?? ""}
            onChange={(event) => update({ availabilityType: event.target.value || undefined })}
          >
            <option value="">Adopt or foster</option>
            {AVAILABILITY_FILTERS.map((value) => (
              <option key={value} value={value}>
                {value === "Adoption" ? "Adopt" : "Foster"}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="filter-city" label="City">
          <Input
            id="filter-city"
            key={`city-${filters.city ?? ""}`}
            defaultValue={filters.city ?? ""}
            maxLength={100}
            autoComplete="off"
            onBlur={(event) => commitText("city", event.target.value, filters.city)}
            onKeyDown={blurOnEnter}
          />
        </FormField>

        <FormField id="filter-min-age" label="Min age (months)">
          <Input
            id="filter-min-age"
            key={`min-${filters.minAgeMonths ?? ""}`}
            type="number"
            inputMode="numeric"
            min={0}
            max={360}
            defaultValue={filters.minAgeMonths ?? ""}
            onBlur={(event) => commitAge("minAgeMonths", event.target.value, filters.minAgeMonths)}
            onKeyDown={blurOnEnter}
          />
        </FormField>

        <FormField id="filter-max-age" label="Max age (months)">
          <Input
            id="filter-max-age"
            key={`max-${filters.maxAgeMonths ?? ""}`}
            type="number"
            inputMode="numeric"
            min={0}
            max={360}
            defaultValue={filters.maxAgeMonths ?? ""}
            onBlur={(event) => commitAge("maxAgeMonths", event.target.value, filters.maxAgeMonths)}
            onKeyDown={blurOnEnter}
          />
        </FormField>

        <FormField id="filter-sort" label="Sort by">
          <Select
            id="filter-sort"
            key={`sort-${filters.sort}`}
            defaultValue={filters.sort}
            onChange={(event) => update({ sort: event.target.value === "Newest" ? undefined : event.target.value })}
          >
            {SORTS.map((value) => (
              <option key={value} value={value}>
                {SORT_LABELS[value]}
              </option>
            ))}
          </Select>
        </FormField>
      </div>

      <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
        <label className="flex items-center gap-2 text-sm text-stone-800">
          <input
            type="checkbox"
            key={`vaccinated-${filters.vaccinated ?? false}`}
            defaultChecked={filters.vaccinated ?? false}
            onChange={(event) => update({ vaccinated: event.target.checked ? "true" : undefined })}
            className="h-4 w-4 accent-orange-600"
          />
          Vaccinated only
        </label>

        <div className="flex items-center gap-3">
          <span role="status" className="text-sm text-stone-500">
            {pending ? "Updating…" : ""}
          </span>
          {hasActiveFilters(filters) && (
            <Link href="/pets" className={buttonClasses("secondary", "sm")}>
              Reset filters
            </Link>
          )}
        </div>
      </div>
    </form>
  );
}