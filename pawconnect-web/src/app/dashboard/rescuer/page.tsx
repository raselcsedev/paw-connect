import type { Metadata } from "next";
import { RescuerOverview } from "@/components/rescuer/rescuer-overview";

export const metadata: Metadata = { title: "Rescuer dashboard" };

export default function RescuerDashboardPage() {
  return <RescuerOverview />;
}