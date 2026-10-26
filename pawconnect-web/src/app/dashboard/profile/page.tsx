import type { Metadata } from "next";
import { ProfileView } from "@/components/account/profile-view";

export const metadata: Metadata = { title: "Your profile", robots: { index: false } };

export default function ProfilePage() {
  return <ProfileView />;
}