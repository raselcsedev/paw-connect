import { NextResponse, type NextRequest } from "next/server";
import { SESSION_COOKIE } from "@/lib/constants";

// Usability only: it avoids showing protected pages to visitors with no session.
// Real authorization happens in the API on every request.
export function proxy(request: NextRequest) {
  if (request.cookies.has(SESSION_COOKIE)) {
    return NextResponse.next();
  }

  const url = request.nextUrl.clone();
  const target = request.nextUrl.pathname + request.nextUrl.search;

  url.pathname = "/login";
  url.search = "";
  url.searchParams.set("next", target);

  return NextResponse.redirect(url);
}

export const config = {
  matcher: ["/dashboard/:path*"],
};