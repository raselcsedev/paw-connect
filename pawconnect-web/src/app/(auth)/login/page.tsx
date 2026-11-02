// import type { Metadata } from "next";
// import { LoginForm } from "@/components/auth/login-form";

// export const metadata: Metadata = { title: "Sign in" };

// export default async function LoginPage({
//   searchParams,
// }: {
//   searchParams: Promise<{ next?: string }>;
// }) {
//   const { next } = await searchParams;
//   return <LoginForm next={next} />;
// }



import type { Metadata } from "next";
import { DemoAccounts } from "@/components/auth/demo-accounts";
import { LoginForm } from "@/components/auth/login-form";
import { DEMO_PASSWORD, demoAccounts } from "@/lib/server/demo-accounts";

export const metadata: Metadata = { title: "Sign in" };

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string }>;
}) {
  const { next } = await searchParams;

  return (
    <>
      <LoginForm next={next} />
      {process.env.NODE_ENV === "development" && (
        <DemoAccounts accounts={demoAccounts} password={DEMO_PASSWORD} next={next} />
      )}
    </>
  );
}