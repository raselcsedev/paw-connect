import { AdminNav } from "@/components/admin/admin-nav";
import { RequireAuth } from "@/components/auth/require-auth";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireAuth roles={["Admin"]}>
      <AdminNav />
      {children}
    </RequireAuth>
  );
}