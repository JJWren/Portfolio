# Security headers are emitted by the application, not the proxy

The application sent no security header of its own beyond HSTS and cache
control, and Kestrel announced itself. The `X-Frame-Options: SAMEORIGIN` and
`Content-Security-Policy: frame-ancestors 'self'` seen on production
responses were not added by the owner's reverse proxy, as first assumed, but
by ASP.NET Core's own defaults: antiforgery sends the first and the
interactive server render mode the second, and both register their
`OnStarting` callback later in the pipeline than any middleware, so they win
a fill-if-absent race. The project is published for self-hosters, many of
whom run the compose stack with no reverse proxy at all. We add a pure
header composer (`SecurityHeadersRules`) and one middleware
(`SecurityHeadersMiddleware`) that set `X-Content-Type-Options`,
`Referrer-Policy`, `X-Frame-Options: DENY`, a `Permissions-Policy`,
`Cross-Origin-Opener-Policy` and an enforced Content-Security-Policy on
every response, switch off Kestrel's `Server` header, suppress the two
framework defaults through their options
(`AntiforgeryOptions.SuppressXFrameOptionsHeader` and
`ServerComponentsEndpointOptions.ContentSecurityFrameAncestorsPolicy = null`)
so the stricter values are the only ones the app sends, and admit the admin
theme's override `<style>` block by a SHA-256 hash carried on the theme
snapshot rather than a nonce.

## Considered Options

- **Headers at the proxy only** — rejected: a self-hoster running the
  compose stack directly, with no reverse proxy in front, would get none of
  the protection. The app's own headers cover everyone; a proxy in front
  (like the owner's) may still add its own on top, and the duplicates are
  harmless — every Content-Security-Policy header a browser receives is
  enforced and the stricter directive wins, and `frame-ancestors` supersedes
  `X-Frame-Options` wherever both are present.
- **A per-request nonce for the theme override `<style>` block** — rejected:
  Blazor's enhanced navigation keeps the first response's policy in force
  while it patches the document's content in place, so a fresh nonce minted
  on a later navigation never matches what that already-enforced policy
  allows, while a hash of unchanged content always does. A hash also needs
  no per-request state and no plumbing into `App.razor`.
- **`style-src-attr 'unsafe-inline'` for the admin pages** — rejected: it
  would mean two policies to explain and maintain, a strict one for public
  pages and a looser one for `/admin/theme`, for a component (the theme
  editor's preview frame and swatches) that can just as easily take its
  colors through the CSSOM, which `style-src` does not govern. One policy,
  everywhere, with nothing to carve out.

## Consequences

- Every response carries the full header set, in every `SECURITY_CSP_MODE`;
  a self-hoster with no reverse proxy is fully covered from the first
  deploy, with no configuration required.
- The two framework defaults are suppressed rather than overwritten, so the
  app sends exactly one `X-Frame-Options` and one `Content-Security-Policy`.
  A proxy in front (the owner's nginx-proxy-manager included) may still add
  headers of its own; such duplicates are harmless because the stricter
  directive wins and `frame-ancestors` supersedes `X-Frame-Options`, and no
  proxy configuration edit was needed.
- The theme override `<style>` block must stay exactly the one line
  `ThemeRules.StyleHash` hashes (pinned by `AppRazorTests`); the theme
  editor forces a full page reload after a save so the new block and its
  header hash always arrive together, rather than relying on enhanced
  navigation to deliver both consistently.
- No component under `Components/` emits a `style` attribute (pinned by
  `NoInlineStyleTests`); the theme editor's preview frame and swatches carry
  `data-style`/`data-color` instead, and `colorpicker.js`'s `applyDataStyles`
  paints them through the CSSOM after every render.
