"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { meKey, purgeUserData } from "@/hooks/use-auth";
import { ApiError, setUnauthorizedHandler } from "@/lib/api-client";

export function Providers({ children }: { children: React.ReactNode }) {
  const router = useRouter();

  // Created once per browser session. Never create this at module level (it would be shared between users on the server).
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            refetchOnWindowFocus: false,
            retry: (failureCount, error) =>
              failureCount < 2 &&
              !(error instanceof ApiError && error.status >= 400 && error.status < 500),
          },
        },
      }),
  );

  useEffect(() => {
    let handling = false;

    setUnauthorizedHandler(() => {
      if (handling) return;
      handling = true;

      const wasSignedIn = queryClient.getQueryData(meKey) != null;

      queryClient.setQueryData(meKey, null);
      purgeUserData(queryClient);

      toast.error(
        wasSignedIn ? "Your session has expired. Please sign in again." : "Please sign in to continue.",
      );

      const next = window.location.pathname + window.location.search;
      router.replace(`/login?next=${encodeURIComponent(next)}`);

      setTimeout(() => {
        handling = false;
      }, 3000);
    });

    return () => setUnauthorizedHandler(null);
  }, [queryClient, router]);

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}