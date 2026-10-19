import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "@/lib/api-client";

/**
 * Puts the API's field errors on the matching form fields.
 * Returns a message for anything that isn't tied to a field ("" when everything was matched).
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly string[],
): string {
  if (!(error instanceof ApiError)) {
    return "Something went wrong. Please try again.";
  }

  let matched = false;

  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    const name = key.charAt(0).toLowerCase() + key.slice(1);

    if (fields.includes(name) && messages[0]) {
      setError(name as Path<T>, { type: "server", message: messages[0] });
      matched = true;
    }
  }

  return matched ? "" : error.message;
}