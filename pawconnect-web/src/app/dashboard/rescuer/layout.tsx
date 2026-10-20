import { RequireAuth } from "@/components/auth/require-auth";
import { RescuerNav } from "@/components/rescuer/rescuer-nav";

export default function RescuerLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireAuth roles={["Rescuer"]}>
      <RescuerNav />
      {children}
    </RequireAuth>
  );
}