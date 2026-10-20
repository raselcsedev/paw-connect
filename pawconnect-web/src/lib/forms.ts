import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "@/lib/api-client";

/**
 * Puts the API's field errors on the matching form fields.
 * `aliases` maps an API field name to a different form field (for example ageInMonths to ageYears).
 * Returns a message for anything that isn't tied to a field ("" when everything was matched).
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly string[],
  aliases: Record<string, string> = {},
): string {
  if (!(error instanceof ApiError)) {
    return "Something went wrong. Please try again.";
  }

  const unmatched: string[] = [];
  let matched = false;

  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    const apiName = key.charAt(0).toLowerCase() + key.slice(1);
    const name = aliases[apiName] ?? apiName;
    const message = messages[0];

    if (!message) continue;

    if (fields.includes(name)) {
      setError(name as Path<T>, { type: "server", message });
      matched = true;
    } else {
      unmatched.push(message);
    }
  }

  if (unmatched.length > 0) return unmatched.join(" ");

  return matched ? "" : error.message;
}