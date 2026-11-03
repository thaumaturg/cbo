# JWT in localStorage, revoked through the security stamp

The API authenticates with a bearer JWT that the SPA keeps in `localStorage` and sends in the `Authorization` header. The SPA reads the user's name and email straight from the token's claims. Any script on the page can read `localStorage`, so an XSS bug, in our code or in a dependency, could steal a token and reuse it for its full 14 days. We accept that because the app holds little of value, and a stolen token can't take over the account: changing the password requires the current one, and there is no way to change the email. Every token carries the user's ASP.NET Identity security stamp, and the JWT bearer handler rejects it once the user is gone or the stamp has changed. That makes leaked tokens revocable without a token table.

## Considered options

- **httpOnly, Secure, SameSite cookie**: scripts can't read the token, so XSS can't steal it, although it can still send requests as the user while the page is open. It needs CSRF protection, the backend has to issue and read the cookie and clear it on logout, and the SPA needs a `/me` endpoint because it can no longer decode the token. That is the recommended step if the app ever holds sensitive data.
- **Backend for frontend**: the same benefit as the cookie, plus a server-side session. Too much infrastructure for one SPA and one API.
- **Short-lived access tokens with refresh tokens**: limits how long a stolen token works, but needs refresh-token storage and rotation, which brings back the same question of where to store it.

## Consequences

- Changing the password rotates the stamp and revokes every token of the user. ChangePassword returns a fresh token so the current tab stays logged in.
- "Log out everywhere" rotates the stamp on demand. Revocation is per user, not per device.
- Every authenticated request costs one user lookup by primary key.
- Tokens without a stamp claim are rejected, so everyone had to log in again once after this change.
- A revoked token gets a 401, and the SPA's response interceptor ends the session.
- HTTPS and signature checks don't help against XSS. The frontend uses no `v-html`, and a Content-Security-Policy header in Caddy (not set yet) is the cheapest next hardening step.
