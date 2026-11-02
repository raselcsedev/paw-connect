"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { useLogin } from "@/hooks/use-auth";
import { errorMessage } from "@/lib/errors";
import { safeRedirect } from "@/lib/redirect";

type DemoAccount = { label: string; email: string; description: string };

export function DemoAccounts({
  accounts,
  password,
  next,
}: {
  accounts: DemoAccount[];
  password: string;
  next?: string;
}) {
  const router = useRouter();
  const login = useLogin();
  const [busy, setBusy] = useState<string | null>(null);

  async function signInAs(account: DemoAccount) {
    setBusy(account.email);

    try {
      await login.mutateAsync({ email: account.email, password });
      toast.success(`Signed in as ${account.label}.`);
      router.replace(safeRedirect(next));
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setBusy(null);
    }
  }

  return (
    <section aria-labelledby="demo-heading" className="mt-8 border-t border-stone-200 pt-6">
      <h2 id="demo-heading" className="text-sm font-semibold text-stone-900">
        Demo accounts
      </h2>
      <p className="mt-1 text-xs text-stone-500">Shown in development only. One click signs you in.</p>

      <ul className="mt-3 space-y-2">
        {accounts.map((account) => (
          <li key={account.email} className="flex items-center justify-between gap-3 rounded-lg bg-stone-50 p-3">
            <div className="min-w-0">
              <p className="text-sm font-medium text-stone-900">{account.label}</p>
              <p className="truncate text-xs text-stone-500">{account.description}</p>
            </div>
            <Button
              size="sm"
              variant="secondary"
              loading={busy === account.email}
              disabled={busy !== null}
              onClick={() => void signInAs(account)}
            >
              Sign in
            </Button>
          </li>
        ))}
      </ul>
    </section>
  );
}