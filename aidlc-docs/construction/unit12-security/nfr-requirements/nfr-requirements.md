# Unit 12 — NFR Requirements: security headers and rate limiting

**Written**: 2026-09-13 after the grilling session. **Depth**: standard. **Inputs**: requirements section 4.3 (FR-D1 to FR-D16) as amended by section 10 (the grilling decisions and the code survey), `inception/application-design/unit12-security-components.md`, the code as of master `ec107a0` with Unit 11 assumed merged (no public component emits a `style` attribute). Every value below is the one the code pins in tests; changing one is a deliberate edit of the rule and its test.

## 1. Headers: the threat each one closes

| Header | Value | Threat closed | Evidence today |
|---|---|---|---|
| `Content-Security-Policy` | section 2 | Injected script or style executing, data leaving for another origin, the site framed elsewhere. Comments and contact bodies already go through the hardened markdown pipeline; blog posts are admin-authored raw HTML; the policy is the second wall behind both. | No CSP is set by the app; the production proxy adds only `frame-ancestors 'self'`. |
| `X-Content-Type-Options` | `nosniff` | A browser sniffing an upload or a response into a script or HTML type. | Uploads are validated by extension only, SVG included. |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Full URLs (admin paths, query strings) leaking to other origins through outbound links and hot-linked images. | The footer's external links and the project redirects. |
| `X-Frame-Options` | `DENY` | Clickjacking in a browser without CSP frame-ancestors. The proxy's `SAMEORIGIN` copy is harmless: frame-ancestors governs where both exist, and a conflict falls back to DENY in Chrome. | Not set by the app. |
| `Permissions-Policy` | `accelerometer=(), browsing-topics=(), camera=(), display-capture=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), screen-wake-lock=(), usb=(), xr-spatial-tracking=()` | Powerful features the site never uses, so an injected script or a framed third party cannot ask for them. FR-D1's five are inside the list. | Not set. |
| `Cross-Origin-Opener-Policy` | `same-origin` | A cross-origin document keeping a handle on the site's window. Sign-in is redirect-based, so nothing legitimate needs the opener. | Not set. |
| `Server` | removed | Kestrel fingerprinting for self-hosters without a proxy. | `AddServerHeader` left at the default; the production proxy overwrites it with its own name. |
| `Strict-Transport-Security` | unchanged, `UseHsts()` outside Development, 30 days | FR-D6: not part of this unit. | Already sent; the proxy passes it through. |

Not adopted, with reasons: `Cross-Origin-Embedder-Policy` (would block the hot-linked HTTPS images the owner allowed in D-Q1); `Cross-Origin-Resource-Policy` (would stop the owner's own images from being embedded elsewhere, which nobody asked for); `X-XSS-Protection` and `Expect-CT` (obsolete); `upgrade-insecure-requests` (breaks a self-hoster on a plain-HTTP LAN address).

Every response gets the set: pages, endpoints, static assets, the error page and the re-executed 404, in every `SECURITY_CSP_MODE`. The middleware fills a header only when the response does not already carry it, so the `/uploads` policy set earlier by the static-file middleware wins for those responses.

## 2. The Content Security Policy, directive by directive

| Directive | Value | Why this value | Evidence |
|---|---|---|---|
| `default-src` | `'self'` | Everything not named below is same-origin only. | Every asset is self-hosted; no CDN, no fonts service. |
| `base-uri` | `'self'` | Nothing may retarget relative URLs. | `App.razor` sets `<base href="/">`. |
| `object-src` | `'none'` | No plugins. | None used. |
| `frame-ancestors` | `'none'` | The site is never framed. Both this and the proxy's `'self'` apply, the stricter wins. | No iframe or embed anywhere; the admin theme preview is inline markup. |
| `form-action` | `'self'` | Forms post only to the site. | The contact form posts to its own URL; the logout form posts to `/auth/logout` and redirects within the origin; the blog filter form is a GET to `/blog`; the sign-in links are GET anchors, so the provider redirect is not a form action. |
| `script-src` | `'self'` | Every script is a file: `theme.js`, `blazor.web.js`, `prism.js`, `site.js`, the reconnect-modal module, and the three admin modules loaded by interop. The three JSON-LD blocks are data blocks the browser never executes. The import map was the only inline script and is removed. No `'unsafe-eval'`: Blazor Server does not need it. | The code survey of 2026-09-13. |
| `style-src` | `'self' 'sha256-<hash>'` | `app.css`, `Portfolio.Web.styles.css`, and the theme override block allowed by the hash of its content, present only when the admin theme overrides something. No `'unsafe-inline'` anywhere: the theme editor's colors move to the CSSOM. | `App.razor` lines 38 to 47; `ThemeEditor.razor` lines 118 and 171 today. |
| `img-src` | `'self' blob: https:` | Uploads, the owner photos and the logo; the crop editor's object URLs; hot-linked HTTPS images in posts (D-Q1, B). | `crop.js` `URL.createObjectURL`. |
| `font-src` | `'self'` | Three self-hosted woff2 files. | `wwwroot/fonts/`. |
| `connect-src` | `'self'` | The circuit's WebSocket and its long-polling fallback, enhanced-navigation fetches, the crop editor's fetch of an existing upload. `'self'` matches the same-origin `wss:` scheme in current browsers; an old browser that refuses it falls back to long polling on the same origin. | `blazor.web.js` default start; `crop.js` line 522. |
| `manifest-src` | `'self'` | `site.webmanifest`. | `App.razor` line 51. |
| not set | `worker-src`, `media-src`, `child-src`, `frame-src` | They fall back to `default-src 'self'`; nothing uses them (Prism's worker path is never taken). | `site.js` calls `highlightElement` without the async flag. |
| not set | `report-uri`, `report-to` | FR-D5: the browser console is the report. | |

**Mode** (`SECURITY_CSP_MODE`, FR-D5): `enforce` (default, also for blank or unknown values) sends `Content-Security-Policy`; `report-only` sends `Content-Security-Policy-Report-Only` with the same value; `off` sends neither. The other headers are always sent.

**`/uploads/*`** (FR-D4): `Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; sandbox` and `X-Content-Type-Options: nosniff`, set in the static-file `OnPrepareResponse` next to the existing `Cache-Control`. A scripted SVG opened directly cannot run script (`sandbox` without `allow-scripts`, `default-src 'none'`); its own styles still render.

## 3. Rate limits: targets, windows, keys

| Target | Policy | Window | Limit | Key | On rejection |
|---|---|---|---|---|---|
| `/auth/login/{provider}`, `/auth/external-callback`, `/auth/logout` | `auth` on the route group | 1 minute | 10 | client address | 429, `Retry-After`, a short text body |
| `/signin-github`, `/signin-google`, `/signin-discord` (the OAuth handlers' own paths, not endpoints) | the global limiter, path-scoped | 1 minute | 10 | client address | same |
| `/feed.xml`, `/sitemap.xml` | `feeds` | 1 minute | 30 | client address | same |
| `/resume`, `/go/{id}/{kind}` | `redirects` | 1 minute | 30 | client address | same; the event is never recorded |
| every other path | the global limiter's no-limit partition | | | | never rejected |
| comment posting (over the circuit) | `CommentLimiter` | 10 minutes | 5 | `user:<id>`, or `ip:<address>` for an anonymous comment; admins exempt | the inline message with the wait; the draft kept |
| report submission (over the circuit) | `ReportLimiter` | 10 minutes | 3 | same | same, in the report form |
| contact form | `ContactRateLimiter` | 10 minutes | 3 | client address | unchanged (FR-D11) |

Semantics: fixed windows (the framework's fixed-window limiter for the endpoints with no queue and automatic replenishment; the generalized in-process limiter for the circuit actions), one limiter per distinct key, all in process memory and gone on restart (FR-D13; a single-container assumption stated in the README). `Retry-After` carries the whole seconds until the window resets, at least 1. A rejection happens after routing and before authentication and analytics, so it is never a page view and never a Named Event (FR-D10, NFR-15). `/healthz`, static assets, the Razor pages, `/robots.txt`, `/site.webmanifest`, `/owner-photo`, `/owner-photo-flip` and `/uploads` are never limited.

The 429 body is written by the limiter's rejection handler, so the status-code re-execute (which only fires for body-less responses) never turns it into the 404 page.

## 4. The proxy trust boundary

- **Today**: `KnownProxies` and `KnownIPNetworks` are cleared, which makes the forwarded-headers middleware honour `X-Forwarded-For` from every peer. Behind the proxy that is right; on the published port a direct client can name any address and become it for the contact limiter and the analytics Visitor Key.
- **After 12b**: `TRUSTED_PROXIES` (comma-separated addresses or CIDR networks) populates the two lists; only a listed peer's `X-Forwarded-For` is honoured, every other connection keeps its own address. Blank keeps today's behaviour so no self-hoster breaks; the README says so and recommends setting it. `ForwardLimit` stays 1 (one proxy hop, the proxy's own value). Junk entries are skipped with one log line at startup.
- **Consequence**: every address-keyed thing moves together: the endpoint policies, the circuit limiters' anonymous key, the contact limiter, the Visitor Key.
- **Production values** (grilling Q10): `TRUSTED_PROXIES=172.22.0.0/16` (the docker network shared with nginx-proxy-manager) and `WEB_BIND=127.0.0.1` (the published port serves this machine only; the proxy reaches the container over the network), set by the session at the 12b deploy.
- **The circuit's client address**: the static blog-post page reads the connection address from its `HttpContext` (already resolved by the forwarded-headers middleware) and passes it to the comment island as a parameter. Interactive-server component parameters travel in a data-protected descriptor, so the browser cannot alter them; a circuit reconnect keeps the original value. Signed-in users key on their id and never need it.

## 5. Non-functional checklist (NFR-10 to NFR-15)

| Id | How this unit meets it |
|---|---|
| NFR-10 | The plain landing page's HTML is unchanged: headers only. Pinned by the existing render tests. |
| NFR-11 | No package: `Microsoft.AspNetCore.RateLimiting`, `ForwardedHeaders`, Kestrel options and `SHA256` are all in the shared framework. |
| NFR-12 | 0 warnings; every new rule tested; two PRs with `feat:` titles from the allow-list. |
| NFR-13 | No owner fact: tests use invented addresses (documentation ranges `192.0.2.0/24`, `198.51.100.0/24`, `203.0.113.0/24`) and invented users. The production values above live only in the docs and the production `.env`. |
| NFR-14 | The security review checks the header values against the OWASP Secure Headers project, exercises the CSP in a browser on the FR-D8 pages, and reads the key path end to end (proxy trust, forwarded address, partition, circuit parameter). |
| NFR-15 | Nothing stored changes; a rejected request is not a page view; limiter keys live in memory only. |

## 6. Verification

- **Headers with curl** (local, then production after the deploy): `curl -sI http://localhost:8080/` shows every header of section 1 and the CSP of section 2 (or the report-only name); `curl -sI http://localhost:8080/uploads/<file>` shows the uploads policy and `nosniff`; `curl -sI http://localhost:8080/no-such-page` (the re-executed 404) shows the set; `curl -sI http://localhost:8080/healthz` is never 429.
- **CSP in a browser** with the console open and `enforce` on (FR-D8): `/` under both flavors, a blog post with code, the comment section (interactive circuit, markdown preview), `/contact` submit, `/signin`, `/feed.xml`, the 404 page, plus enhanced navigation between them and a full reload: zero violations.
- **429 walk-through**: a loop of 31 requests to `/feed.xml` in a minute ends with 429, `Retry-After` and the text body; the next minute answers 200 again.
- **The owner's production check** (grilling Q7): the first deploy runs `report-only`; the owner opens `/admin/theme` (swatches, the color picker, a mode toggle, a save), `/admin/site` (an upload and the crop editor), `/admin/stats` and `/admin/posts` with the console open and reports zero CSP reports; the session then sets `enforce` and recreates the container. The checklist ships in `construction/build-and-test/security-verification-instructions.md`.
