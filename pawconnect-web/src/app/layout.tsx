import type { Metadata } from "next";
import { Geist } from "next/font/google";
import { Toaster } from "sonner";
import { Navbar } from "@/components/layout/navbar";
import { Providers } from "@/components/providers";
import "./globals.css";

const geist = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });

export const metadata: Metadata = {
  title: { default: "PawConnect", template: "%s · PawConnect" },
  description: "Adopt or foster a pet from independent rescuers and small shelters.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={`${geist.variable} flex min-h-screen flex-col font-sans`}>
        <Providers>
          <Navbar />
          <main className="flex flex-1 flex-col">{children}</main>
          <footer className="border-t border-stone-200 py-6 text-center text-sm text-stone-500">
            © {new Date().getFullYear()} PawConnect
          </footer>
        </Providers>
        <Toaster richColors closeButton position="top-right" />
      </body>
    </html>
  );
}