import { handleAuth } from "@/lib/server/auth-handler";

export async function POST(request: Request): Promise<Response> {
  return handleAuth(request, "/api/auth/register", 201);
}