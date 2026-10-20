"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { FormField } from "@/components/forms/form-field";
import { Pagination } from "@/components/pets/pagination";
import { Badge } from "@/components/ui/badge";
import { Button, buttonClasses } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useAdminUsers, useSetUserStatus } from "@/hooks/use-admin";
import { useCurrentUser } from "@/hooks/use-auth";
import { ACCOUNT_STATUSES, hasUserFilters, USER_ROLES, usersHref, type UserFilters } from "@/lib/admin/filters";
import { ACCOUNT_TONES } from "@/lib/admin/format";
import { errorMessage } from "@/lib/errors";
import { formatDate } from "@/lib/pets/format";
import type { AccountStatus, AdminUserDto } from "@/types/admin";
import type { Role } from "@/types/api";

function UserActions({ user, isSelf }: { user: AdminUserDto; isSelf: boolean }) {
  const setStatus = useSetUserStatus();
  const [open, setOpen] = useState(false);

  if (isSelf) return <span className="text-xs text-stone-500">This is you</span>;

  const disabling = user.status === "Active";

  async function confirm() {
    try {
      await setStatus.mutateAsync({ id: user.id, status: disabling ? "Disabled" : "Active" });
      toast.success(disabling ? `${user.displayName} was disabled.` : `${user.displayName} was reactivated.`);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setOpen(false);
    }
  }

  return (
    <>
      <Button
        size="sm"
        variant={disabling ? "secondary" : "primary"}
        aria-label={`${disabling ? "Disable" : "Reactivate"} ${user.displayName}`}
        onClick={() => setOpen(true)}
      >
        {disabling ? "Disable" : "Reactivate"}
      </Button>
      <ConfirmDialog
        open={open}
        title={disabling ? `Disable ${user.displayName}?` : `Reactivate ${user.displayName}?`}
        message={
          disabling
            ? "They are signed out immediately and cannot sign in until you reactivate them. Their listings and applications stay as they are, so moderate any listings separately."
            : "They can sign in again straight away."
        }
        confirmLabel={disabling ? "Disable account" : "Reactivate account"}
        confirmVariant={disabling ? "danger" : "primary"}
        loading={setStatus.isPending}
        onConfirm={() => void confirm()}
        onCancel={() => setOpen(false)}
      />
    </>
  );
}

export function UsersView({ filters }: { filters: UserFilters }) {
  const router = useRouter();
  const { data: me } = useCurrentUser();
  const { data, isPending, isError, error, refetch, isPlaceholderData } = useAdminUsers(filters);
  const go = (overrides: Partial<UserFilters>) => router.push(usersHref(filters, overrides));

  function commitSearch(raw: string) {
    const value = raw.trim();
    if (value !== (filters.q ?? "")) go({ q: value || undefined });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-stone-900">Users</h1>
        <p className="mt-1 text-sm text-stone-600">Email addresses are shown only so you can identify accounts. Please treat them as confidential.</p>
      </div>

      <form
        role="search"
        aria-label="Filter users"
        onSubmit={(event) => event.preventDefault()}
        className="grid gap-4 rounded-2xl border border-stone-200 bg-white p-4 sm:grid-cols-2 lg:grid-cols-4"
      >
        <FormField id="user-q" label="Search by name or email">
          <Input
            id="user-q"
            key={`q-${filters.q ?? ""}`}
            type="search"
            defaultValue={filters.q ?? ""}
            maxLength={100}
            autoComplete="off"
            onBlur={(event) => commitSearch(event.target.value)}
            onKeyDown={(event) => event.key === "Enter" && event.currentTarget.blur()}
          />
        </FormField>

        <FormField id="user-role" label="Role">
          <Select
            id="user-role"
            key={`role-${filters.role ?? ""}`}
            defaultValue={filters.role ?? ""}
            onChange={(event) => go({ role: (event.target.value || undefined) as Role | undefined })}
          >
            <option value="">Any role</option>
            {USER_ROLES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </FormField>

        <FormField id="user-status" label="Account status">
          <Select
            id="user-status"
            key={`status-${filters.status ?? ""}`}
            defaultValue={filters.status ?? ""}
            onChange={(event) => go({ status: (event.target.value || undefined) as AccountStatus | undefined })}
          >
            <option value="">Any status</option>
            {ACCOUNT_STATUSES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </Select>
        </FormField>

        <div className="flex items-end">
          {hasUserFilters(filters) && (
            <Link href="/dashboard/admin/users" className={buttonClasses("secondary", "md")}>
              Reset filters
            </Link>
          )}
        </div>
      </form>

      {isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : isPending ? (
        <div role="status" aria-label="Loading users" className="animate-pulse space-y-2">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-14 rounded-xl bg-stone-200" />
          ))}
        </div>
      ) : data.totalCount === 0 ? (
        <EmptyState
          title="No users match your filters"
          action={
            <Link href="/dashboard/admin/users" className={buttonClasses("secondary")}>
              Reset filters
            </Link>
          }
        />
      ) : data.items.length === 0 ? (
        <EmptyState
          title="That page does not exist"
          action={
            <Link href={usersHref(filters, { page: 1 })} className={buttonClasses("secondary")}>
              Go to the first page
            </Link>
          }
        />
      ) : (
        <section aria-busy={isPlaceholderData}>
          <p aria-live="polite" className="mb-3 text-sm text-stone-600">
            {data.totalCount} {data.totalCount === 1 ? "user" : "users"}
          </p>
          <div className="overflow-x-auto rounded-2xl border border-stone-200 bg-white">
            <table className="w-full min-w-[32rem] text-left text-sm">
              <caption className="sr-only">Users</caption>
              <thead className="bg-stone-50 text-xs uppercase tracking-wide text-stone-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">Name</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium md:table-cell">Email</th>
                  <th scope="col" className="px-4 py-3 font-medium">Role</th>
                  <th scope="col" className="px-4 py-3 font-medium">Status</th>
                  <th scope="col" className="hidden px-4 py-3 font-medium lg:table-cell">Joined</th>
                  <th scope="col" className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-stone-100">
                {data.items.map((user) => (
                  <tr key={user.id}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-stone-900">{user.displayName}</p>
                      <p className="break-all text-xs text-stone-500 md:hidden">{user.email}</p>
                    </td>
                    <td className="hidden break-all px-4 py-3 text-stone-700 md:table-cell">{user.email}</td>
                    <td className="px-4 py-3">
                      <Badge>{user.role}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge tone={ACCOUNT_TONES[user.status]}>{user.status}</Badge>
                    </td>
                    <td className="hidden px-4 py-3 text-stone-700 lg:table-cell">{formatDate(user.createdAtUtc)}</td>
                    <td className="px-4 py-3">
                      <UserActions user={user} isSelf={user.id === me?.id} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(target) => usersHref(filters, { page: target })} />
        </section>
      )}
    </div>
  );
}