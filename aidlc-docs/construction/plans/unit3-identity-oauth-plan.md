# Unit 3 Plan — Identity + OAuth (GitHub / Google / Discord)

External-login-only auth (no passwords). Providers register conditionally from env creds so
self-hosters can enable any subset. Admin = OAuth email listed in ADMIN_EMAILS → Admin role.
/admin is role-gated and renders as 404 for everyone else (hidden, not just forbidden).

- [x] Packages: Microsoft.AspNetCore.Authentication.Google 10.0.10, AspNet.Security.OAuth.GitHub/Discord 10.0.0
- [x] AdminEmails service (comma-separated env parse, case-insensitive match) + 10 test cases
- [x] Identity core (AddIdentityCore + roles + SignInManager + Identity cookies); LoginPath=/signin
- [x] Conditional provider registration from OAUTH__{PROVIDER}__CLIENTID/CLIENTSECRET; OAuthProviders service
- [x] Auth endpoints: GET /auth/login/{provider}, GET /auth/external-callback (create-or-link by email,
      display-name refresh, Admin role sync each sign-in, open-redirect guard), POST /auth/logout (antiforgery)
- [x] Seed Admin role at startup after Migrate()
- [x] Forwarded-headers middleware (KnownIPNetworks — KnownNetworks is deprecated in .NET 10)
- [x] Blazor auth wiring: cascading auth state, AuthorizeRouteView (NotAuthorized → NotFoundContent)
- [x] /signin page (provider picker; error messages; friendly note when none configured)
- [x] Nav: AuthorizeView — Admin link (Admin role only), sign in / sign out controls
- [x] /admin dashboard shell (role-gated placeholder cards)
- [x] Verify: build + 15 tests green; compose: /signin 200, anon /admin → 302 /signin, non-admin → 404,
      no Admin link leak. Full OAuth round-trip untestable without real client creds (user action needed).
