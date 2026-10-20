import type { Metadata } from "next";
import { AdminOverview } from "@/components/admin/admin-overview";

export const metadata: Metadata = { title: "Administration", robots: { index: false } };

export default function AdminPage() {
  return <AdminOverview />;
}