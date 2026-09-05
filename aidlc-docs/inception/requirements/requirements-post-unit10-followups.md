# Requirements: post-Unit 10 follow-ups (theme-toggle titles, current belt, security headers and rate limiting)

**Written**: 2026-09-04, the session after Unit 10 closed (release v1.25.1). **Owner and only stakeholder**: Joshua Mykitta. **Depth** (adaptive): item B minimal, item C standard, item D standard with a security emphasis. **Sources**: `construction/plans/unit10-closeout-handoff-2026-09-04.md`; `construction/plans/unit10-bjj-landing-plan.md` (Phase 5 deferred item, "Out of scope"); `construction/unit10-bjj-landing/functional-design/business-rules.md` (BR-3, BR-4, BR-6, BR-9, BR-15, BR-18); `inception/reverse-engineering/code-quality-assessment.md` ("Engineering"); `inception/reverse-engineering/architecture.md` (pipeline order); `src/Portfolio.Web/Program.cs`; `docs/adr/0002-belt-colors-are-fixed-constants.md`; `README.md`; `docker-compose.yml`. Section 5 of `requirements.md` (NFR-1 to NFR-9) still applies to everything below.

## 1. The ask (verbatim)

The session offered the pick-up candidates from the close-out handoff as five options: A) the owner deploy steps as a paste-ready checklist; B) the deferred theme-toggle titles under the BJJ flavor with the wording "Switch to the white gi (light theme)" and "Switch to the black belt (dark theme)"; C) a follow-up unit for a "current belt" field so the rank bar can draw a belt other than black; D) a follow-up unit for security headers and rate limiting from the reverse-engineering pass; E) something else (per-node landing sections, kids and coral belts, the fonts cache lifetime, or a new request).

Owner's answer:

```text
A) okay
B) sure
C) yes
D) yes
E) not sure what you want here... this seems more like a deferment issue
```

## 2. Intent analysis

- **A** is owner work, not code. Delivered as `construction/unit10-bjj-landing/owner-deploy-checklist.md` (six steps plus rollback, values copied from the plan's content sheet). No requirements needed; it is listed here only so the audit trail is complete.
- **B** closes the last open box of the Unit 10 plan (Phase 5, "Optional, owner's call"). The wording is now chosen, so the item is a tiny, fully specified change: a tooltip that names the theme the button switches to, in BJJ terms, only under the BJJ flavor. The `aria-label` stays functional.
- **C** removes the v1 limitation BR-6 recorded: the rank bar always draws a black belt. The site is published for self-hosters, so the flavor must not assume the owner's rank; a "current belt" field lets any practitioner use it, with black remaining the default so every existing deployment renders exactly as today.
- **D** is the defence-in-depth gap the reverse-engineering pass called out twice: zero security headers (no CSP, `nosniff`, `Referrer-Policy`, `Permissions-Policy` or frame protection) and no framework rate limiting (only the in-memory contact-form limiter; nothing on the auth endpoints, comment posting or report submission). Both are pipeline plumbing in `Program.cs` with the same test style and the same deployment consideration (the reverse proxy), so they share one unit in two PRs.
- **E** means nothing further now. Per-node landing sections, kids' and coral belts and the longer cache lifetime for `/fonts/*` stay deferred as recorded in the plan.

## 3. Units of work

| Unit | Title | Commit type | Size | Depends on |
|---|---|---|---|---|
| Unit 10 addendum (Phase 6) | BJJ wording for the theme-toggle tooltip | `feat:` | XS, one PR | nothing |
| Unit 11 | Current belt for the rank bar | `feat:` | S, one PR with a migration | nothing |
| Unit 12 | Security headers and rate limiting | `feat:` twice (12a headers, 12b rate limiting) | M, two PRs | 12a is easier after Unit 11 removes the rank bar's inline `style` attribute |

Suggested order: B, C, 12a, 12b. Each unit runs under the orchestration model recorded on 2026-09-04 (one Sonnet phase agent, five review agents, remediation, PR, Copilot gate, squash-merge).

## 4. Functional requirements

### 4.1 Unit 10 addendum: theme-toggle tooltip (B)

| Id | Requirement |
|---|---|
| FR-B1 | Under `SiteFlavor.Bjj` the server-rendered `<html>` element carries `data-flavor="bjj"`. Under `Default` it carries no such attribute, so the plain page's markup stays byte-for-byte unchanged. |
| FR-B2 | Under the BJJ flavor the theme toggle's `title` names the theme the click switches to: while the page is dark, "Switch to the white gi (light theme)"; while light, "Switch to the black belt (dark theme)". Under `Default` the title stays the server-rendered "Switch theme" and the script leaves it alone. |
| FR-B3 | The title is correct at first paint, after each toggle, and after every Blazor enhanced navigation (the same re-apply hook `site.js` already uses to restore `data-theme`). |
| FR-B4 | The `aria-label` ("Switch between dark and light theme") is unchanged; the tooltip is a courtesy for pointer users, not the accessible name. |
| FR-B5 | No new script file, no inline handler (the `NoInlineOnClickTests` pin stays green), no owner fact (BR-18). The two strings are pinned by a test so a wording change is a deliberate edit. |

### 4.2 Unit 11: current belt for the rank bar (C)

| Id | Requirement |
|---|---|
| FR-C1 | A new "Current belt" value from the closed set white, blue, purple, brown, black (the existing `Belt` enum; kids' and coral belts stay out of scope). Stored in `SiteContent.CurrentBelt` (nullable text), with the env fallback `SITE_CURRENT_BELT` parsed into `SiteConfig`. Resolution follows BR-3: admin override, then env, then the default. |
| FR-C2 | The default is black. A deployment that never sets the field renders the rank bar exactly as v1.25.1 does. |
| FR-C3 | Drawing rule per belt (colors stay the fixed `:root` constants of ADR 0002; no new theme token): body and tip in the belt's `--rank-*` color; the stripe bar in `--belt-black` with `--belt-white` stripes for white, blue, purple and brown; the black belt keeps its red bar (`--c-red`), matching `.belt-band.black .bar` on the road. The `--border` ring stays so the white belt reads on the light theme. |
| FR-C4 | `BeltDegrees` keeps its column, range (0 to 6) and env name; only its admin hint changes to say "stripes (degrees on a black belt)". No data migration of existing values. |
| FR-C5 | BR-9 generalizes from "the black-belt era" to "the last era on the current belt": when such an era exists and degrees are set, its stripes must equal the degrees; the save is refused with a message that names the belt. Lenient at resolve (BR-4): a mismatch never hides the bar. |
| FR-C6 | Validation at save: an unknown belt is refused with "Current belt must be white, blue, purple, brown or black". At resolve an unknown stored or env value falls back to the default (black), never to a broken page. |
| FR-C7 | The rank bar's inline `style="background: var(--c-red)"` moves into `app.css` (a class or `data-belt` per belt). After this unit no public-page component in `Components/` emits a `style` attribute, which Unit 12a relies on. |
| FR-C8 | Admin editor: a select in the BJJ group beside "Belt caption" and "Belt degrees", with a blank option labelled as the default (black), shown only under the BJJ flavor (BR-1). `SiteContentDraft` gains a trailing positional `CurrentBeltText`; every construction site uses named arguments (the Phase 4 convention). |
| FR-C9 | Migration `AddCurrentBelt`; `.env.example` (blank by default, generic example comment); README env-table row; `CONTEXT.md` glossary entry "Current belt"; the Unit 10 plan's "Out of scope" bullet marked done. |
| FR-C10 | Tests: env parsing (valid, blank, unknown); resolve precedence and the black default; validation messages; the generalized degrees check (era on the current belt, era on another belt, no eras); render tests for each belt class, the red bar only for black, the stripe count, and no `style` attribute in the rank bar; `AppCssTests` pins that a rule exists for every belt and that the reduced-motion block is unchanged (no new animation). |

### 4.3 Unit 12: security headers and rate limiting (D)

#### 12a: security headers

| Id | Requirement |
|---|---|
| FR-D1 | Every response carries `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY`, a `Permissions-Policy` that denies at least camera, microphone, geolocation, payment and USB, and `Cross-Origin-Opener-Policy: same-origin` (sign-in is redirect-based, never a popup). Kestrel's `Server` header is switched off. |
| FR-D2 | A Content Security Policy, enforced by default, with at least: `default-src 'self'`, `base-uri 'self'`, `object-src 'none'`, `frame-ancestors 'none'`, `form-action 'self'`, `script-src 'self'` (no `'unsafe-inline'`, no `'unsafe-eval'`), `font-src 'self'`, `manifest-src 'self'`, `img-src 'self' blob: https:` (the admin crop preview uses object URLs; any HTTPS image is allowed so hot-linked images in posts keep working, owner decision D-Q1 of 2026-09-04), and a `connect-src` that admits the same-origin Blazor circuit over WebSockets in every supported browser. |
| FR-D3 | Inline CSS is handled without `'unsafe-inline'` in `script-src` and, for the public pages, without it in `style-src` either: the admin theme override `<style>` block in `App.razor` gets a per-request nonce; the admin theme editor's dynamic `style` attributes (preview frame and swatches, applied by Blazor through `setAttribute`) get whichever of `style-src-attr` allowance or a class-based refactor the design chooses, with the security review weighing it. Scripts set styles through the CSSOM (`element.style.x = ...`), which CSP allows. |
| FR-D4 | Responses under `/uploads/*` (user-supplied files, including SVG validated by extension only) carry their own restrictive policy (`default-src 'none'; style-src 'unsafe-inline'; sandbox`) plus `nosniff`, closing the scripted-SVG finding without changing upload validation. |
| FR-D5 | An env switch `SECURITY_CSP_MODE` with values `enforce` (default), `report-only` and `off` selects how the CSP header is emitted; the other headers are always on. This is the rollout and rollback story for a self-hoster whose setup differs (a sub-path, an unusual proxy). No report collection endpoint: the browser console is the report. |
| FR-D6 | HSTS stays as it is (`UseHsts()` in non-development, the 30-day default). Lengthening it or adding `includeSubDomains` is the owner's call later, not part of this unit. |
| FR-D7 | The header set is composed by a pure, unit-tested class (each header and every CSP directive pinned) and emitted by one small middleware placed so static files, endpoints and Blazor pages all get it. The README's "Running behind a reverse proxy" section states that the app sets its own headers and the proxy may add but need not. ADR 0003 records "security headers are emitted by the application, not the proxy". Because the owner's production proxy already adds `X-Frame-Options: SAMEORIGIN` and a `Content-Security-Policy: frame-ancestors 'self'` (evidence below), the NFR design must avoid a conflicting duplicate: browsers ignore `X-Frame-Options` when two values disagree and enforce every CSP header they receive, so either the app matches those two values or the README tells the owner which proxy options to switch off. |

**Production evidence (read-only header check, 2026-09-04, after the Unit 10 deploy)**: the response for `/` arrives through an openresty reverse proxy and already carries `Strict-Transport-Security: max-age=2592000` (the app's `UseHsts()` default passed through), `Content-Security-Policy: frame-ancestors 'self'` and `X-Frame-Options: SAMEORIGIN`; no `X-Content-Type-Options`, `Referrer-Policy` or `Permissions-Policy`. The `Server` header reads `openresty`, so FR-D1's Kestrel change matters for self-hosters without such a proxy.
| FR-D8 | Verification in a browser with zero CSP violations on: `/` under both flavors, a blog post with code (Prism), the comment section (interactive circuit, markdown preview), `/contact` submit, `/signin`, `/feed.xml`, the 404 page. Admin pages (`/admin/theme` with the preview frame and color picker, `/admin/site` uploads, the crop editor) cannot be exercised locally without admin OAuth, so the design must make them verifiable by the owner in production with `report-only` as the fallback, and the checklist for that is part of the unit's docs. |

#### 12b: rate limiting

| Id | Requirement |
|---|---|
| FR-D9 | Framework rate limiting (`AddRateLimiter` / `UseRateLimiter`, in-memory, no new package) with named per-client-IP policies on the minimal-API endpoints: the auth group (`/auth/login/{provider}`, `/auth/external-callback`, `/auth/logout`), the DB-backed feeds (`/feed.xml`, `/sitemap.xml`) and the counted redirects (`/resume`, `/go/{id}/{kind}`). `/healthz` (the compose health check) and static assets are exempt. No global limiter in this unit. |
| FR-D10 | A rejected request gets 429 with `Retry-After` and a small body, and must not fall into `UseStatusCodePagesWithReExecute("/not-found")` (which re-executes body-less error responses). Rejections happen before the analytics middleware, so they are never counted as page views. |
| FR-D11 | The contact form keeps `ContactRateLimiter` (per IP, 3 per 10 minutes); it is a Razor page POST, not an endpoint a named policy can target, and its behaviour is already right. |
| FR-D12 | Comment posting and report submission, which travel over the interactive circuit rather than as HTTP requests, get application-level fixed-window limits keyed by user id (signed in) or by the client IP captured when the circuit starts (anonymous comments): defaults 5 comments per 10 minutes and 3 reports per 10 minutes; admins exempt. The limiter is the existing contact limiter generalized (keyed, `TimeProvider`-driven, tested with `FakeTimeProvider`). The UI shows a friendly inline message with the wait time and keeps the draft text. |
| FR-D13 | Limits are process memory and reset on restart, like the contact limiter; documented as a single-container assumption. No Redis, no database table. |
| FR-D14 | The client IP that keys every limit is the forwarded address only when it comes from a trusted proxy. Today `KnownProxies` and `KnownIPNetworks` are cleared, so any client reaching port 8080 directly could forge `X-Forwarded-For` and evade an IP-keyed limit. A new env `TRUSTED_PROXIES` (comma-separated IPs or CIDRs) populates them; blank keeps today's trust-everything behaviour so no self-hoster breaks, and the README states the trade-off and recommends setting it. The owner sets it to the proxy's network at deploy time. |
| FR-D15 | Compose gains `WEB_BIND` (default `0.0.0.0`, unchanged behaviour) so the published port can be bound to loopback when the proxy runs on the same host: `"${WEB_BIND:-0.0.0.0}:${WEB_PORT:-8080}:8080"`. Documented next to `TRUSTED_PROXIES`. |
| FR-D16 | Tests: policy option builders (partition key, window, permit count) and rejection body; the generalized limiter (allow, deny, window roll-over, key isolation); the trusted-proxy parsing (blank, IPs, CIDRs, junk); the comment and report services refusing over the limit and exempting admins; pipeline-order pins where a text scan of `Program.cs` is the cheapest guard (rate limiter after routing, before authentication; analytics after). |

### 4.4 Quick change (owner request, 2026-09-04): pinned site header

Owner's words, given with the workflow plan approval: "make a quick change to let the navbar at the top stay locked at the top when a user scrolls down/up." Depth: minimal (intent only). Slot: PR 0, ahead of the planned PRs. Plan: `construction/plans/quick-pinned-header-plan.md`.

| Id | Requirement |
|---|---|
| FR-Q1 | The site header (logo, nav links, theme toggle, mobile burger) stays visible at the top of the viewport while the page scrolls in either direction, on every page that uses the main layout, at every width. |
| FR-Q2 | Sticky, not fixed: the header keeps its place in normal flow, so no content shifts and no body offset is needed. BR-13 stays true because the header is outside `LandingSections` and the admin theme preview never renders it; the fixed-position facts in `AppCssTests` stay green. |
| FR-Q3 | The header is opaque (the theme's page-background token, so the admin theme and the light mode both recolor it) with its existing bottom border, so scrolled content never shows through; no blur. It stacks above page content and below the Blazor error bar. |
| FR-Q4 | In-page anchor jumps and keyboard focus scrolling land below the header: `scroll-padding-top` on the root equal to the header height plus a small gap, expressed through one layout constant that the admin editor's sticky preview panel (today `top: 1rem`) also uses to clear the header. |
| FR-Q5 | The mobile menu panel keeps dropping from the header's bottom edge and stays attached while scrolled; the burger and the theme toggle keep working with no script change. |
| FR-Q6 | No new animation or transition (NFR-6 unaffected); the plain landing page's HTML is unchanged (NFR-10); one CSS pin in `AppCssTests` keeps the sticky rule, the root scroll padding and the preview offset from regressing silently. |

## 5. Non-functional requirements (in addition to NFR-1 to NFR-9)

| Id | Requirement |
|---|---|
| NFR-10 | The plain landing page with `SITE_FLAVOR` unset stays byte-for-byte identical in its HTML. Response headers are the only thing Unit 12 changes for it. |
| NFR-11 | No new NuGet package: rate limiting, forwarded headers and header emission are in the shared framework. |
| NFR-12 | `dotnet build -warnaserror` with 0 warnings; `dotnet test` green (697 tests today) plus the new tests; each PR sized for the Copilot gate with a title from the CI allow-list. |
| NFR-13 | BR-18 holds: no owner fact in code, tests, `.env.example` or docs examples. Unit 11 examples use invented belts and gyms. |
| NFR-14 | The five-area review's security pass checks the header values against the OWASP Secure Headers recommendations, exercises the CSP in a browser, and reads the limiter key path end to end (proxy trust, forwarded address, partition). |
| NFR-15 | Nothing here changes what the analytics pipeline stores (ADR 0001): a rejected request is not a page view, and no new personal data is persisted (limiter keys live in memory only). |

## 6. Decisions taken by default (say so if you disagree)

1. **Blank current belt means black** (FR-C2), not "the last era's belt" and not "hide the bar". It keeps every existing deployment unchanged and keeps the bar independent of the road (BR-2: sections hide independently).
2. **Env name `SITE_CURRENT_BELT`**, matching the column and the glossary term, the way `SITE_ERAS` matches `Eras`.
3. **CSP enforced by default** with `SECURITY_CSP_MODE` as the escape hatch, rather than shipping report-only first. The public pages are verified locally before the PR; the admin pages are the owner's production check with `report-only` one env edit away.
4. **HSTS unchanged** in this unit.
5. **No global rate limiter**: targeted policies on the endpoints that can be abused, plus per-user limits where the circuit carries the action. A global limiter would count static assets and punish shared IPs for no gain.
6. **`TRUSTED_PROXIES` blank keeps today's behaviour**; locking it down is a deploy-time owner action, documented.
7. **Limit numbers are code constants** (like the contact limiter), not env settings.

## 7. Open question for the owner

**D-Q1, image sources for the CSP (FR-D2).** Blog posts are rendered through the trusted markdown pipeline, so a post can hot-link an image from another site; comments cannot (images collapse to alt text). Which policy?

- A) Same-origin only: `img-src 'self' blob:`. Any post that hot-links an external image stops showing it until the image is uploaded through the editor. Strictest, and the recommended default for a site that already hosts every image it uses.
- B) Any HTTPS image: `img-src 'self' blob: https:`. Hot-linked images keep working; the residual risk (an injected image URL leaking a reader's address to a third party) is small but real.

**Answer (owner, 2026-09-04)**: B. FR-D2 carries `img-src 'self' blob: https:`.

**Approval (owner, 2026-09-04)**: "A", continue to Workflow Planning with the requirements and the section 6 defaults as written.

## 8. Out of scope (recorded)

- Per-node landing sections for Guard, Pass, Mount, Submit; kids' and coral belts; a longer cache lifetime for `/fonts/*` (deferred by the owner on 2026-09-04).
- Validating or re-encoding SVG uploads (FR-D4 contains the risk instead).
- A CSP report collection endpoint; output caching and response compression; structured logging (the other engineering notes of the reverse-engineering pass).
- Any change to HSTS (FR-D6).

## 9. Approval

Requirements analysis complete. The next stage is Workflow Planning (phases, depths and PR sequence for the three units), followed by construction unit by unit.
