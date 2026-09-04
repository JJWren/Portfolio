# Code Quality Assessment

Snapshot at `master` @ `e1825d9` (v1.22.0), 2026-09-03. Three angles: engineering, front-end/UX/accessibility, and product completeness for a personal portfolio. Findings cite `file:line` under `src/Portfolio.Web/` unless stated. Nothing here is softened.

## Bottom line

This is a genuinely well-engineered application that is an under-finished portfolio. The engineering discipline is above what most production teams ship: zero TODO/FIXME comments across 74 commits, a two-pipeline markdown sanitizer with an AST-level scheme guard, decompression-bomb-safe image handling, honest privacy-preserving analytics with an ADR, a real release pipeline, and code comments that explain root causes. The verification story is the weak half (250 pure-logic tests that never touch a component, an endpoint, or a query), and the runtime is missing the standard hardening tier (CSP, `nosniff`, framework rate limiting, output caching, structured logging). The content side is better than the repository suggests: the live site carries a real tagline, About, skills, portrait, meta description and résumé through admin overrides and the production environment, none of which is visible from the repository or the local `.env` (verified against the live pages on 2026-09-03 and 2026-09-04). What remains is presentation: the Brazilian Jiu-Jitsu black belt, the one thing no other senior .NET developer's portfolio has, is one About paragraph, one skill chip and one blog post, with no visual or structural treatment on the landing page.

## Pros

### Engineering

- **Layout and pinning are boring in the good way.** `Portfolio.slnx` with `src/Portfolio.Web` + `tests/Portfolio.Tests`; `global.json` pins SDK 10.0.302 with `rollForward: latestFeature`; `dotnet-tools.json` pins `dotnet-ef` 10.0.10 to match the EF packages; both projects enable `Nullable` and `ImplicitUsings` with no escape hatches.
- **Zero TODO / FIXME / HACK / XXX comments in the source tree** (case-insensitive grep over `src/` and `tests/`; the only hits are HTML `placeholder=` attributes and strings inside the blocklist).
- **No secrets committed.** `appsettings.json` holds logging levels and `AllowedHosts` only; the dev connection string is a localhost throwaway; `.env` is gitignored and only `.env.example` is tracked.
- **CI is real.** `.github/workflows/ci.yml` builds with `-warnaserror` (which also makes NuGet audit findings fatal; this is how a transitive AngleSharp CVE surfaced and got a dependency removed), runs tests, and gates PR titles to conventional commits. `release-please.yml` publishes to GHCR only when a release is created and re-runs `dotnet test` against the tag first.
- **EF Core state is healthy.** 18 migrations plus the model snapshot committed, coherent names (`InitialIdentity` → `AddOwnerPhoto`), applied at startup (`Program.cs:152`).
- **Production plumbing is thought through.** Health check with DbContext probe (`Program.cs:144-145, 240`) wired to the compose healthcheck; data-protection keys on a volume (`Program.cs:63-68`); forwarded headers for TLS termination (`Program.cs:71-76`); HSTS outside development (`Program.cs:200`); `UseAntiforgery()` with `<AntiforgeryToken />` on the logout form (`Program.cs:217`, `Components/Layout/MainLayout.razor:53`).
- **The markdown security split is good work.** `Services/MarkdownService.cs` keeps a trusted pipeline for admin posts and a UGC pipeline with `DisableHtml`, hand-picked extensions, and a `DocumentProcessed` guard that strips images, applies a default-deny URL scheme check (including protocol-relative and backslash variants), and tags survivors `rel="nofollow ugc noopener"`.
- **Redirects are safe.** Open-redirect guard on `returnUrl` (`Endpoints/AuthEndpoints.cs:115-119`); `/go/{id}/{kind}` sources URLs only from admin-edited columns and re-validates with `ProjectUrlRules.IsHttp` (`Endpoints/AnalyticsEndpoints.cs:24-32`).
- **Images defend against decompression bombs.** Byte-capped buffering, `Image.IdentifyAsync` plus a decoded-size guard before full decode, re-encode to WebP (which also strips EXIF) in `AvatarService` and `OwnerPhotoService`.
- **Error handling is deliberate.** Catches are narrowly filtered or logged with context; `EmailService.cs` documents why SMTP failure is non-fatal (the message is already stored).
- **Comments explain why.** `Program.cs:26-29` (why an explicit factory for the blocklist), `:47-55` (why `EnableDynamicJson`), `:168-170` (RFC 9110 HEAD semantics), `:210-213` (why `UseRouting` is explicit).

### Front end, accessibility, SEO, performance

- `prefers-reduced-motion: reduce` honoured (`wwwroot/app.css:282-285`).
- Global `:focus-visible` ring in brand blue (`app.css:121-125`) with a deliberate carve-out for `FocusOnNavigate`'s programmatic heading focus (`app.css:130-132`, `Components/Routes.razor:9`).
- The nav collapses at 720px; the burger carries `aria-label`, `aria-expanded`, `aria-controls`, and `site.js` keeps `aria-expanded` in sync across enhanced navigation (`MainLayout.razor:15-16`, `wwwroot/js/site.js`).
- Landmarks complete and labelled: `<header>`, `<main>`, `<footer>`, three `<nav>` elements (`Main`, `Social`, `Legal`).
- Alt text handled with thought: `SeoRules.NormalizeAltText` never returns null (Blazor would omit the attribute); decorative images get `alt=""`; blog thumbnails additionally get `tabindex="-1" aria-hidden="true"` to avoid duplicate tab stops.
- Every icon is `aria-hidden="true"` and every icon-only control has an accessible name (theme toggle, carousel arrows, admin edit link, pin indicator).
- Custom widgets are keyboard-operable: the colour picker's saturation/value area is a `role="slider"` with arrow-key handling; crop zoom is a native range input.
- Contrast is computed, not eyeballed (`Services/ThemeRules.cs:292-340`). Shipped tokens: `#a49d91` on `#151515` ≈ 7.3:1; light `#6b6459` on `#f6f3ec` ≈ 4.9:1.
- Live regions used correctly (`role="alert"` for errors, `role="status"` for confirmations).
- Performance basics: self-hosted woff2 with `font-display: swap`, no CDN, `<ResourcePreloader />`, `defer` on `prism.js` and `site.js`, `loading="lazy"` on below-fold list images but not on the post hero (the LCP element), `/uploads` served immutable because filenames are single-use GUIDs.
- Theme applied pre-paint by `theme.js` in `<head>` with `localStorage` access wrapped in try/catch.
- SEO is the most complete part of the front end: per-page `<PageTitle>` and `<SocialMeta>`, canonical and `og:url` with the query stripped so filter/pager variants self-canonicalize, `twitter:card`, absolute `og:image` with a neutral fallback card, article times, JSON-LD (`Person`, `WebSite`, `BlogPosting`), `/sitemap.xml`, `/robots.txt`, `/feed.xml`, a generated `site.webmanifest` that picks up admin theme colours, and real 404 status codes.

### Product

- The blog is the strongest feature: drafts, tags, reading time, header images with alt text, search + month + tag filters, pagination, bundled syntax highlighting, RSS.
- Analytics is a differentiator: first-party, cookieless, script-free, DNT/GPC-honouring, daily-rotating HMAC visitor keys, 90-day raw retention rolled into permanent aggregates, an admin dashboard, and an ADR.
- Contact-form spam defence is disproportionately good: honeypot, signed render timestamp, per-IP rate limit, MX validation, an 8.2k-domain disposable list, and a confidence split (fake success for hard signals, quarantine for soft ones).
- The admin CMS is extensive: posts, projects, comments, reports, messages, site copy, palette, stats; all sortable, filterable, paginated, and role-gated.
- Legal pages ship (`/terms`, `/privacy`) and the privacy page honestly describes the actual analytics mechanics.

## Cons

### Engineering

- **No `.editorconfig`, no `Directory.Build.props`.** Warning strictness lives only in `ci.yml:24`; a local `dotnet build` does not fail on warnings and nothing pins `TreatWarningsAsErrors`, `AnalysisLevel`, or `EnforceCodeStyleInBuild`. `CONTRIBUTING.md` asks contributors to pass `-warnaserror` by hand.
- **Dead scaffold test shipped.** `tests/Portfolio.Tests/UnitTest1.cs` is an empty `[Fact]` that asserts nothing.
- **`ThemeService` swallows every non-cancellation exception with no logger.** The class takes only `IDbContextFactory`; a DB or serialization failure silently degrades the site's palette with zero telemetry. `Program.cs:48-51` documents that this guard already hid a real bug (missing `EnableDynamicJson`).
- **`.svg` uploads are validated by extension only** (`Services/ImageUploadService.cs`), written verbatim, and served same-origin from `/uploads` with `immutable` caching. With no CSP and no `nosniff`, a scripted SVG would execute on-origin. Mitigation: the only caller is the admin-only `ImageCropField`, so this is a compromised-admin path. It is still inconsistent with `AvatarService` and `OwnerPhotoService`, which re-encode.
- **Zero security headers.** No `Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, or `X-Frame-Options` anywhere in `src/`. For an app that renders admin-authored raw HTML through the trusted pipeline, CSP is the missing defence-in-depth layer.
- **No framework rate limiting.** `AddRateLimiter` / `UseRateLimiter` are absent. The only throttle is the in-memory contact-form limiter, which also resets on every restart. `/signin`, `/auth/login/{provider}`, comment posting, and report submission have none.
- **No output caching and no response compression.** Every hit on `/`, `/blog`, `/projects` re-queries Postgres and re-renders; `/feed.xml` and `/sitemap.xml` hit the DB per request and emit no cache headers.
- **Observability is console-only.** Stock logging levels, no structured sink, no request correlation, no OpenTelemetry. `AnalyticsService` deliberately swallows failures into warnings that nobody can see.
- **Everything is a singleton** (`Program.cs:17-45`). It works because every service takes the context factory, but `ThemeService` and `SiteContentService` already hold in-process caches justified by "single-container deploy", a constraint the README never states and which breaks silently with two replicas.
- **No component, integration, or end-to-end tests.** 250 `[Fact]`/`[Theory]` methods, all pure logic. No bUnit, Playwright, or `WebApplicationFactory`. `aidlc-docs/construction/build-and-test/integration-test-instructions.md` and `performance-test-instructions.md` exist as documents with no code. The riskiest paths (OAuth callback, analytics middleware, migrations, SEO endpoints) are verified only by manual `curl` runs recorded in `audit.md`.
- **No Dependabot or Renovate.** All package versions are hand-pinned; vulnerability scanning happens only incidentally through `-warnaserror`.
- **Three stale local branches** (`feature/admin-notifications`, `fix/admin-projects-links-badge`, `fix/focus-and-theme`) plus a merged remote branch (`origin/docs/85-readme-truth-pass`).

### Front end, accessibility, UX

- **No skip link (WCAG 2.4.1).** Grep for "skip" over `.razor` and `.css` returns nothing and `<main>` has no `id`. Keyboard and screen-reader users tab through up to nine header controls on every page.
- **The mobile nav has no focus management and no Escape handler.** `site.js` toggles a class and `aria-expanded`; focus stays on the burger. It is driven by an inline `onclick` (`MainLayout.razor:16`), as are `__toggleTheme()` (`MainLayout.razor:63`) and `__scrollProjects()` (`Pages/Projects.razor:36, 97`): four global functions on inline handlers that a future CSP `script-src` would break.
- **The projects carousel is not keyboard-scrollable.** The `<ul class="carousel">` has `overflow-x: auto` and no `tabindex="0"`; a project with neither URL has no focusable descendant and is unreachable by keyboard.
- **`#blazor-error-ui` dismiss is a bare `<span>` with the `🗙` emoji** (`MainLayout.razor:116`): not a button, not focusable, announced literally. The only emoji in the codebase.
- **No `width`/`height` on any `<img>`.** `.owner-photo` is saved by `aspect-ratio`; `.project-image`, `.post-hero-image`, and blog thumbs rely on layout-time sizing (measurable CLS on the blog list).
- **Effectively one breakpoint (720px).** Between 721px and the 1080px container cap, `.about-grid` (`1.6fr 1fr`) and `.contact-grid` stay two-column and the hero keeps its 320px photo track; no large-desktop treatment.
- **The crop tool's box is pointer-only.** `crop.js` has no `keydown` handler and the crop frame has no `tabindex` (WCAG 2.1.1) on the widget the owner uses most.
- **Contrast passes AA narrowly on the least legible type.** `--text-muted` clears 4.5:1 but is applied at 0.75-0.8rem uppercase mono with 0.08em tracking (nav, timestamps, skill chips, admin headers). `.crop-guide` text is hardcoded `rgba(255,255,255,0.75)` outside the token system.
- **The theme editor warns but never blocks** unreadable palettes (`ThemeRules.ContrastWarnings` is advisory).
- **The honeypot wrapper is an ARIA-validity edge case**: `aria-hidden="true"` on a container holding a form control (`Pages/Contact.razor:68-73`); mitigated by `tabindex="-1"`.
- **The render-mode choice has an unacknowledged cost.** Every interactive island opens a SignalR circuit, so a blog post page holds a server circuit for every reader who reaches the comments; the site cannot be CDN-served and readers see a reconnect prompt on every container restart. `blazor.web.js` loads without `defer` while its neighbours have it.
- **Hero heading FOUT.** Fraunces loads with `font-display: swap` and no preload, so the h1 repaints on font arrival.
- **`ReconnectModal.razor.css` is stock and untokenized** (white background, `#6b9ed2` buttons): the one visitor-facing surface that ignores the theme.

### Content and documentation

- **The local `.env` still carries the template's tagline and skills** (`SITE_TAGLINE` and `SITE_SKILLS` byte-identical to `.env.example`) and a blank `PUBLIC_BASE_URL`. Production overrides the copy from `/admin/site` and resolves canonical, `og:url` and the sitemap pointer to `https://joshuamykitta.dev` correctly, so these are dev-environment nits, not live defects; aligning them keeps local runs looking like production.
- **One demo post is still published.** "A quick markdown tour" from `DemoSeeder` sits on `/blog` next to the real posts (the other seeded post was rewritten). The local `.env` also leaves `SEED_DEMO_DATA=true`, which is harmless because the seeder only fills empty tables.
- **The résumé is live but one link deep.** `/resume` serves `Joshua-Mykitta-Resume.pdf` in production (`RESUME_FILE` is set there, not locally), yet the only link to it is one `<li>` in the contact aside; nothing in the nav or hero.
- **README claims a CI publish job that does not exist.** `ci.yml` has no publish step; `release-please.yml` publishes only on release creation. This survived the commit titled "README truth pass".
- **Only one ADR.** OAuth-only auth, all-singleton DI, single-container caches, global InteractiveServer, Postgres over SQLite have no ADR; their rationale is scattered through `aidlc-state.md` and a 300-line `audit.md`.
- **Design rationale lived outside the repository** (`aidlc-state.md` points at a plan file under the owner's home directory). This reverse-engineering set is the first in-repo record.
- **Stale counters** in `aidlc-state.md` and `construction/build-and-test/unit-test-instructions.md` (184 tests / 23 fixtures versus 250 methods / 35 files).
- **Privacy wording is defensible but loose.** `Pages/Privacy.razor:20-23` says statistics are recorded "with nothing running in your browser" while `blazor.web.js`, `prism.js`, and `site.js` do run; the accurate claim is "no analytics script".

## Gaps (absent things, ranked by impact for this portfolio)

1. **The black belt has words but no treatment.** On the live site it is one About paragraph ("black belt instructor with over 20 years of experience"), one "Brazilian Jiu-jitsu" skill chip, the meta description's "occasional jiu-jitsu metaphor", and one post ("Positions Before Decisions: What Brazilian Jiu-Jitsu Taught Me About Software Architecture"). In the repository, a grep for `jiu|jitsu|bjj|belt|grappl|martial|dojo|academy` finds nothing in source, markup or styles. No photo from the mat, no belt history, nothing in the structured data, no landing-page device. A black belt is a decade-plus of work and a rare differentiator for a senior .NET engineer; the landing page gives it a sentence.
2. **The résumé is unreachable from the nav or hero.** It exists and is served (see Cons); recruiters have to find the Contact page first.
3. **No experience / timeline / work history** anywhere in the data model or UI.
4. **No project detail pages.** `Data/Project.cs` is title, summary, image, two URLs, order, visibility. A carousel of one-line cards cannot demonstrate senior-level work.
5. **One demo post still live** ("A quick markdown tour"); unpublish or replace it.
6. **Local `.env` placeholders** (tagline, skills, blank `PUBLIC_BASE_URL`, `SEED_DEMO_DATA=true`) make dev runs look unlike production; align them.
7. **No About entry in the nav** (`MainLayout.razor:23-25` offers Projects / Blog / Contact); the about content is undiscoverable except by scrolling the home page.
8. **JSON-LD `Person` is nearly empty**: name, origin, `sameAs`, photo; no `jobTitle`, `description`, `knowsAbout`.
9. **No testimonials or recommendations.**
10. **No hire-me CTA or availability signal.** The hero offers GitHub, LinkedIn, Get in touch.
11. **No `/uses`, `/now`, speaking or writing page.**
12. **No integration or E2E test suite** (instruction docs exist without code).
13. **No architecture doc** until this pass.
14. **No subscribe path beyond RSS.**

## Phase 0 content checklist (no code, no design decision required)

Verified against the live site on 2026-09-04; the first draft of this table was written from the local `.env` and overstated the problems.

| Item | Live status | Action |
|---|---|---|
| Canonical, `og:url`, sitemap origin | Correct (`https://joshuamykitta.dev`) | None in production. Set `PUBLIC_BASE_URL` in the local `.env` so dev output matches |
| Tagline, About, skills | Real copy via `/admin/site` | None. Optionally copy the live values into the local `.env` |
| Meta description | Set ("Secure software engineer building .NET and self-hosted things ... the occasional jiu-jitsu metaphor.") | None |
| Demo content | "A quick markdown tour" still published on `/blog`; projects are all real | Unpublish or replace the post; `SEED_DEMO_DATA=false` locally is cosmetic |
| Résumé | `/resume` serves `Joshua-Mykitta-Resume.pdf`; linked only from the Contact aside | Add a nav or hero link (part of Unit 10) |
| Owner photo | Live portrait served | None |
| `unit-test-instructions.md`, `aidlc-state.md` | Stale counts | Corrected in `aidlc-state.md`; `unit-test-instructions.md` still to do |

## Constraints that shape any landing-page change (verified)

| Constraint | Evidence | Consequence |
|---|---|---|
| `LandingSections.razor` renders on `/` and inside an `inert aria-hidden` half-width frame | `Components/Admin/ThemeEditor.razor:118-120`, `app.css:1873-1890` | Nothing `position: fixed` inside `LandingSections`; interactive elements must look finished at rest |
| Public pages are static SSR; JS is first-party progressive enhancement | `Program.cs`, `wwwroot/js/site.js` | No CDN, no third-party scripts, no InteractiveServer on `/` |
| "The same image works for anyone" | `README.md:3-5`, blank-hides pattern in `LandingSections.razor:36` | Owner-specific content must be data-driven or behind an env switch |
| The preview frame carries only the 15 token vars | `Services/ThemeRules.cs:269-283`, pinned by `ThemeRulesTests.cs:280-295` | New fixed constants on `:root` render in the preview without touching the 26-token catalog |
| One keyframe and one reduced-motion block | `app.css:266-285` | Every new animation registers there; scroll-driven animations need `@supports` |
| Brand red fails text contrast on dark | `app.css:51` comment, ≈ 2.9:1 | Belt red may carry stripes, never text |
| Fraunces ships upright 600-700 only | `app.css:4-10` | No italic display type |
| The dark theme is already a black belt; the light theme is a gi | `app.css:31-75` | The strongest design lever needs no new colours |
