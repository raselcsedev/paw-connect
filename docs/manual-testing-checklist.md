# PawConnect manual testing checklist

Automated tests cover the rules. This checklist covers what they can't: real browsers, real size limits, real file serving, and the feel of the product. Run it before each release.

## Setup
- [ ] XAMPP MySQL running; `dotnet run --project PawConnect.Api --launch-profile https`; `npm run dev` in `pawconnect-web`
- [ ] Accounts: one adopter, two rescuers, the admin (Swagger Authorize needs the token only, without "Bearer")

## Swagger (https://localhost:7227/swagger)
### Authentication
- [ ] Register adopter and rescuer: 201 and tokens. Register with role Admin: 400
- [ ] Login with a wrong password: 401, generic message. Five wrong passwords then the right one: still 401 (locked)
- [ ] `GET /api/auth/me` without a token: 401 with `WWW-Authenticate: Bearer`. With a token: 200
- [ ] Refresh with the same refresh token twice: second call 401 and the session is dead
- [ ] 11 logins within a minute: 429 with `Retry-After`

### Pets and visibility
- [ ] Create a pet as a rescuer: 201, status Draft. Anonymous `GET /api/pets/{id}`: 404
- [ ] Publish: now visible in `GET /api/pets` and by id
- [ ] Edit as another rescuer: 403 (published) and 404 (draft)
- [ ] Filters: species, size, min/max age, availability, city, q, sort, page, pageSize=51 (400)

### Applications
- [ ] Adopter applies: 201. Apply again: 409. Foster without dates: 400
- [ ] Rescuer approves with the right version: 200. Same request again (old version): 409
- [ ] Approve a second applicant for the same pet: 409. Reject the first, then approve the second: 200
- [ ] Complete adoption: pet becomes Adopted, other open applications become Rejected, notifications arrive
- [ ] Withdraw a Pending application: 200. Withdraw once under review: 409

### Photos (needs a real image file)
- [ ] Upload JPEG, PNG, WebP: 201. Upload a .txt renamed to .jpg: 400
- [ ] Upload a file larger than 6 MB: 413 (this is enforced by the real server, not by the test server)
- [ ] Open the photo URL in a browser: the image shows; response headers include `X-Content-Type-Options: nosniff`
- [ ] Draft listing: photo URL as anonymous gives 404

### Moderation
- [ ] Report a listing, report it again (409), report your own (409)
- [ ] Admin resolves with Suspend: listing disappears publicly; owner sees the reason and cannot publish
- [ ] Restore: listing stays paused until the owner publishes

## Postman
1. Create an environment with `baseUrl = https://localhost:7227` and `token` (empty). Turn off "SSL certificate verification" for localhost only.
2. In the login request, add this under Tests: `pm.environment.set("token", pm.response.json().accessToken);`
3. In the collection, set Authorization to Bearer Token with `{{token}}`.
4. Make one folder per role (Adopter, Rescuer, Admin) so you can switch tokens quickly.
5. Add Tests for status codes, for example `pm.test("is 403", () => pm.response.to.have.status(403));`.

## Browser (http://localhost:3000)
- [ ] Register, sign in, sign out; the cookies `pc_access` and `pc_refresh` are HttpOnly and `document.cookie` shows neither
- [ ] Delete `pc_access` and reload a dashboard: still signed in (silent refresh)
- [ ] Browse with filters; copy the URL into a new tab: same results
- [ ] Apply for a pet; withdraw; see the status history
- [ ] Rescuer: create a listing, add photos, publish, review an application, complete an adoption
- [ ] Admin: disable a user (they are signed out at once), review a report, suspend and restore a listing
- [ ] Notifications bell updates within a minute; mark one read; mark all read
- [ ] 375 px width and keyboard-only passes on every page above
