import { RequireAuth } from "@/components/auth/require-auth";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireAuth>
      <div className="mx-auto w-full max-w-6xl px-4 py-8">{children}</div>
    </RequireAuth>
  );
}