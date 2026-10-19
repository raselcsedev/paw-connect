import { RequireAuth } from "@/components/auth/require-auth";

export default function MyApplicationsLayout({ children }: { children: React.ReactNode }) {
  return <RequireAuth roles={["Adopter"]}>{children}</RequireAuth>;
}