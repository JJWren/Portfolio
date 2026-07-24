# AIDLC State — Portfolio Rebuild

**Project type**: Greenfield (complete rewrite of JJWren/Personal_Portfolio)
**Stack**: ASP.NET Core Blazor Web App (.NET 10), PostgreSQL, Docker Compose
**Approved plan**: C:\Users\joshu\.claude\plans\i-want-to-completely-melodic-treehouse.md (copied decisions below)

## Inception Phase
- [x] Workspace Detection — greenfield; existing repo is static HTML/CSS/JS, content reference only
- [x] Reverse Engineering — skipped (greenfield rewrite; old code intentionally discarded)
- [x] Requirements Analysis — standard depth; decisions captured via two clarification rounds (see audit.md)
- [x] User Stories — skipped (single stakeholder/owner; requirements fully enumerated in approved plan)
- [x] Workflow Planning — approved plan with 8 construction units
- [x] Application Design — captured in approved plan (architecture, entities, auth flow, env config)
- [x] Units Generation — 8 units defined (see below)

## Construction Phase — Units
- [x] Unit 1: Scaffold solution + Docker skeleton (COMPLETE — checkpoint approved 2026-07-20)
  - [x] .NET 10 SDK 10.0.302 installed (winget); global.json pins it
  - [x] Solution scaffolded (Portfolio.slnx, Blazor Web App `src/Portfolio.Web`, xUnit `tests/Portfolio.Tests`)
  - [x] Packages: Npgsql.EFCore.PostgreSQL, Identity.EFCore, EFCore.Design, Markdig; dotnet-ef local tool
  - [x] AppDbContext (IdentityDbContext<ApplicationUser>) + InitialIdentity migration; Migrate() on startup
  - [x] Dockerfile (multi-stage), docker-compose.yml (web + postgres:17-alpine, healthcheck, pgdata/uploads/dpkeys volumes), .dockerignore, .env.example, README stub
  - [x] `dotnet build` + `dotnet test` pass
  - [x] `docker compose up --build` boots app + applies migrations (HTTP 200)
  - [x] Fixes: libgssapi-krb5-2 in runtime image; data-protection keys persisted to volume; HTTPS redirect gated to dev (TLS at reverse proxy)
- [x] Unit 2: Theming, layout, static pages (COMPLETE — see construction/plans/unit2-theming-plan.md)
- [x] Unit 3: Identity + OAuth (COMPLETE — see construction/plans/unit3-identity-oauth-plan.md; OAuth sign-in verified live in production)
- [x] Unit 4: Blog (COMPLETE — see construction/plans/unit4-blog-plan.md)
- [x] Unit 5: Comments + moderation (COMPLETE — see construction/plans/unit5-comments-plan.md)
- [x] Unit 6: Projects (COMPLETE — see construction/plans/unit6-projects-plan.md)
- [x] Unit 7: Contact form (COMPLETE — see construction/plans/unit7-contact-plan.md)
- [x] Unit 8: Extras + polish + CI (COMPLETE — repo live at github.com/JJWren/Portfolio; see construction/plans/unit8-extras-plan.md)
- [x] Build and Test — instruction docs written; CI run 1 SUCCESS (build + 41 tests + GHCR publish); clean-slate compose verified. User-only items listed in build-and-test-summary.md.

## Operations Phase — Post-Launch (2026-07-21 → present)
- [x] Deployed: live at https://portfolio.theguywiththedogs.dev (HTTP container behind reverse proxy, TLS at proxy; image ghcr.io/jjwren/portfolio)
- [x] OAuth apps registered; live sign-in + admin role verified in production
- [x] Production .env personalized; old JJWren/Personal_Portfolio repo archived
- [x] Release flow: squash-merged conventional-commit PRs + release-please tags/GHCR publish; Copilot review gate on every PR (see CONTRIBUTING.md)
- Issue-driven releases v1.1.0 → v1.12.0 (2026-07-21 → 2026-07-24):
  - v1.1.0 comment profiles (display name, avatar) + anonymous commenting
  - v1.2.0 official logo, mobile hamburger nav, local-time timestamps
  - v1.3.0 MIT license, /terms + /privacy pages, configurable sponsor link
  - v1.4.0 blog list images + admin edit shortcut
  - v1.5.0 comment reporting, moderation queue, bans, user messages (+ /messages and /my-reports pages)
  - v1.6.0 blog + admin/user list pagination with search, month, and tag filters
  - v1.7.0 admin notification badges (open reports, unread messages)
  - v1.8.x admin projects UX (stacked links, clickable visibility badge); friendly length validation for posts and projects
  - v1.9.x 16:9 crop-box image editor for blog headers and project cards (shared ImageCropField, per-circuit ES module)
  - v1.10.0 sortable admin columns; project cards deep-link to the editor for admins
  - v1.11.0 admin-editable landing-page content overrides (/admin/site)
  - v1.12.0 inline icons for external links and sign-in providers
- Test suite: 41 → 184 tests, 23 fixtures (see construction/build-and-test/unit-test-instructions.md)
- Full history in CHANGELOG.md (release-please); decision log in audit.md

## Key Decisions
- OAuth-only sign-in (no passwords); providers conditionally registered from env creds
- Admin = email in ADMIN_EMAILS env (comma-separated) → Admin role claim at sign-in
- Contact: SMTP send + DB copy, admin inbox
- Projects: DB-backed admin CRUD (image, title, homepage/repo, summary), carousel UI
- Theme: dark-first (#151515/#A63D40/#E9B872/#90A959/#6494AA), light toggle
- Self-hoster story: all personal details via env vars; image published to GHCR
