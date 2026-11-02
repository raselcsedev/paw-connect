#!/usr/bin/env bash
# PawConnect pre-flight check. Run from the repository root: bash scripts/preflight.sh
FAILED=0
LOG="${TMPDIR:-/tmp}/pawconnect-preflight"

pass() { printf '  [ok]   %s\n' "$1"; }
fail() { printf '  [FAIL] %s\n' "$1"; FAILED=1; }

echo "Tools"
if dotnet --version >/dev/null 2>&1; then pass "dotnet $(dotnet --version)"; else fail "dotnet is not installed"; fi
if node --version >/dev/null 2>&1; then pass "node $(node --version)"; else fail "node is not installed"; fi

echo "Database server"
if node -e "const s=require('net').connect(3306,'127.0.0.1');s.on('connect',()=>process.exit(0));s.on('error',()=>process.exit(1));setTimeout(()=>process.exit(1),3000)"; then
  pass "something is listening on port 3306"
else
  fail "nothing is listening on port 3306. Start MySQL in the XAMPP Control Panel."
fi

echo "API configuration (values are never printed)"
SECRETS="$(dotnet user-secrets list --project PawConnect.Api 2>/dev/null)"
for key in ConnectionStrings:DefaultConnection Jwt:SigningKey; do
  if echo "$SECRETS" | cut -d' ' -f1 | grep -qx "$key"; then pass "secret $key is set"; else fail "secret $key is missing"; fi
done

DB="$(echo "$SECRETS" | grep '^ConnectionStrings:DefaultConnection' | sed -n 's/.*Database=\([^;]*\).*/\1/p')"
case "$DB" in
  "")      fail "could not read the database name from ConnectionStrings:DefaultConnection" ;;
  *_test)  fail "the API points at '$DB', which is the TEST database. It must be 'paw_connect'." ;;
  *)       pass "the API uses the database '$DB'" ;;
esac

KEYLEN="$(echo "$SECRETS" | grep '^Jwt:SigningKey' | sed 's/^Jwt:SigningKey = //' | tr -d '\n' | wc -c | tr -d ' ')"
if [ "${KEYLEN:-0}" -ge 32 ]; then pass "Jwt:SigningKey has $KEYLEN characters"; else fail "Jwt:SigningKey must have at least 32 characters"; fi

echo "Web app configuration"
if [ -f pawconnect-web/.env.local ]; then pass "pawconnect-web/.env.local exists"; else fail "pawconnect-web/.env.local is missing (copy .env.example)"; fi
grep -q '^API_BASE_URL=' pawconnect-web/.env.local 2>/dev/null && pass "API_BASE_URL is set" || fail "API_BASE_URL is missing in .env.local"
grep -q '^APP_ORIGIN=' pawconnect-web/.env.local 2>/dev/null && pass "APP_ORIGIN is set" || fail "APP_ORIGIN is missing in .env.local"
if [ -f pawconnect-web/certs/aspnet-dev.pem ]; then
  if grep -q 'PRIVATE KEY' pawconnect-web/certs/aspnet-dev.pem; then
    fail "certs/aspnet-dev.pem contains a private key. Delete it and export the certificate again."
  else
    pass "development certificate exported"
  fi
else
  fail "pawconnect-web/certs/aspnet-dev.pem is missing (dotnet dev-certs https --export-path ... --format Pem)"
fi

echo "API build"
if dotnet build PawConnect.Api -v q -nologo >"$LOG-api.log" 2>&1; then
  pass "the API builds"
else
  fail "the API does not build. First errors:"
  grep -E "error" "$LOG-api.log" | sort -u | head -15
fi

echo "Database migrations"
PENDING="$(dotnet ef migrations list --project PawConnect.Api --no-build 2>/dev/null | grep -c '(Pending)')"
if [ "${PENDING:-0}" -eq 0 ]; then pass "no pending migrations (or the database could not be read)"; else fail "$PENDING migration(s) pending. Run: dotnet ef database update --project PawConnect.Api"; fi

echo "Web app type check"
if (cd pawconnect-web && npx tsc --noEmit >"$LOG-web.log" 2>&1); then
  pass "the web app type-checks"
else
  fail "the web app has type errors. First errors:"
  head -15 "$LOG-web.log"
fi

echo
if [ "$FAILED" -eq 0 ]; then echo "All checks passed."; else echo "Some checks failed. Fix the [FAIL] lines above, then run this script again."; fi
exit "$FAILED"
