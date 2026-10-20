"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { Button, buttonClasses } from "@/components/ui/button";
import { useCurrentUser, useLogout } from "@/hooks/use-auth";
import { cn } from "@/lib/cn";
import { NotificationBell } from "@/components/layout/notification-bell";

const publicLinks = [{ href: "/pets", label: "Browse pets" }];

export function Navbar() {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const router = useRouter();
  const { data: user, isPending } = useCurrentUser();
  const logout = useLogout();

  const links = user
    ? [...publicLinks, { href: "/dashboard", label: "Dashboard" }]
    : publicLinks;

  function closeMenu() {
    setOpen(false);
  }

  async function signOut() {
    closeMenu();
    await logout.mutateAsync().catch(() => undefined);
    toast.success("You have been signed out.");
    router.replace("/");
  }

  const linkClass = (href: string) =>
    cn(
      "rounded-lg px-3 py-2 text-sm font-medium hover:bg-stone-100",
      pathname === href || pathname.startsWith(`${href}/`)
        ? "text-orange-700"
        : "text-stone-700",
    );

  const account = isPending ? (
    <div
      className="h-9 w-32 animate-pulse rounded-lg bg-stone-200"
      aria-hidden="true"
    />
  ) : user ? (
    <div className="flex items-center gap-3">
      <NotificationBell />
      <div className="text-right leading-tight">
        <p className="text-sm font-medium text-stone-900">{user.displayName}</p>
        <p className="text-xs text-stone-500">{user.role}</p>
      </div>
      <Button
        variant="secondary"
        size="sm"
        loading={logout.isPending}
        onClick={signOut}
      >
        Sign out
      </Button>
    </div>
  ) : (
    <div className="flex items-center gap-2">
      <Link
        href="/login"
        onClick={closeMenu}
        className={buttonClasses("ghost", "sm")}
      >
        Sign in
      </Link>
      <Link
        href="/register"
        onClick={closeMenu}
        className={buttonClasses("primary", "sm")}
      >
        Create account
      </Link>
    </div>
  );

  return (
    <header className="sticky top-0 z-30 border-b border-stone-200 bg-white/90 backdrop-blur">
      <nav
        aria-label="Main"
        className="mx-auto flex h-16 w-full max-w-6xl items-center justify-between px-4"
      >
        <div className="flex items-center gap-6">
          <Link
            href="/"
            onClick={closeMenu}
            className="text-lg font-bold text-orange-700"
          >
            🐾 PawConnect
          </Link>
          <div className="hidden items-center gap-1 md:flex">
            {links.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={linkClass(link.href)}
              >
                {link.label}
              </Link>
            ))}
          </div>
        </div>

        <div className="hidden md:block">{account}</div>

        <button
          type="button"
          className="rounded-lg p-2 text-stone-700 hover:bg-stone-100 focus-visible:outline-2 focus-visible:outline-orange-600 md:hidden"
          aria-expanded={open}
          aria-controls="mobile-menu"
          aria-label={open ? "Close menu" : "Open menu"}
          onClick={() => setOpen((value) => !value)}
        >
          <svg
            className="h-6 w-6"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            aria-hidden="true"
          >
            {open ? (
              <path d="M6 6l12 12M18 6L6 18" />
            ) : (
              <path d="M4 7h16M4 12h16M4 17h16" />
            )}
          </svg>
        </button>
      </nav>

      {open && (
        <div
          id="mobile-menu"
          className="space-y-2 border-t border-stone-200 bg-white px-4 py-4 md:hidden"
        >
          {links.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              onClick={closeMenu}
              className={cn("block", linkClass(link.href))}
            >
              {link.label}
            </Link>
          ))}
          <div className="pt-2">{account}</div>
        </div>
      )}
    </header>
  );
}
