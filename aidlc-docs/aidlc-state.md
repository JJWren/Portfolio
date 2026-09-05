# AIDLC State — Portfolio Rebuild

**Project type**: Greenfield (complete rewrite of JJWren/Personal_Portfolio)
**Stack**: ASP.NET Core Blazor Web App (.NET 10), PostgreSQL, Docker Compose
**Approved plan**: C:\Users\joshu\.claude\plans\i-want-to-completely-melodic-treehouse.md (copied decisions below)

## Inception Phase
- [x] Workspace Detection — greenfield; existing repo is static HTML/CSS/JS, content reference only
- [x] Reverse Engineering — executed 2026-09-03 at v1.22.0 (brownfield pass; artifacts in inception/reverse-engineering/). The July note "skipped (greenfield rewrite)" is superseded.
- [x] Requirements Analysis (2026-09-03) — portfolio review + BJJ landing directions; see inception/requirements/requirements.md
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
- [x] Unit 10: BJJ-themed landing page — COMPLETE 2026-09-04 with the merge of PR #96 (Phases 1 to 5: PRs #89, #90, #92, #94, #96; 697 tests). Close-out handoff: construction/plans/unit10-closeout-handoff-2026-09-04.md. Owner actions completed 2026-09-04 (checklist confirmed; the BJJ landing is live in production); the deferred owner-call item became the Unit 10 addendum. Brief: construction/plans/unit10-bjj-landing-handoff.md (seven decisions locked 2026-09-04). Design sources: inception/application-design/landing-directions/. Plan: construction/plans/unit10-bjj-landing-plan.md.
  - [x] Functional Design (minimal) — 2026-09-04: construction/unit10-bjj-landing/functional-design/domain-entities.md and business-rules.md (BR-1 to BR-19)
  - [x] NFR Requirements — skipped (constraints fixed in inception/requirements/requirements.md section 5 and the handoff)
  - [x] NFR Design — skipped (no new NFR patterns; same stack)
  - [x] Infrastructure Design — skipped (same image, env switch, no new infrastructure)
  - [x] Code Generation Part 1 (plan) — written and approved 2026-09-04 (functional design approved in the same message); orchestration model: one Sonnet subagent per phase, five-area review after each phase, then PR, Copilot gate, squash-merge
  - [x] Code Generation Part 2 — COMPLETE: Phase 1 merged 2026-09-04 as PR #89 (c1380a2, 445 tests); Phase 2 merged 2026-09-04 as PR #90 (e6a4a75, 567 tests); Phase 3 merged 2026-09-04 as PR #92 (7dddfdf, 666 tests); Phase 4 merged 2026-09-04 as PR #94 (18b1788, 689 tests); Phase 5 PR #96 (perf close-out and docs, 697 tests) merged to complete the unit. Phases: Phase 0 owner actions; Phase 1 test: render safety net; Phase 2 feat: flavor, hero game plan, rank bar, principles, admin fields, ADR 0002; Phase 3 feat: the road and now; Phase 4 feat: second photo slot and portrait switch; Phase 5 perf: preload, delegated handlers, docs
  - [x] Build and Test — construction/build-and-test/unit-test-instructions.md refreshed in Phase 5 (697 tests, 38 fixtures)
- [ ] Post-Unit 10 follow-ups (started 2026-09-04): owner approved A (deploy checklist), B (theme-toggle titles), C (current belt), D (security headers and rate limiting); E nothing, the other follow-ups stay deferred. Requirements: inception/requirements/requirements-post-unit10-followups.md (B minimal, C standard, D standard). Owner checklist for A: construction/unit10-bjj-landing/owner-deploy-checklist.md (delivered 2026-09-04; the owner completed it the same day: SITE_FLAVOR=bjj live in production).
  - [x] Requirements Analysis — approved 2026-09-04 (D-Q1 answered B: any HTTPS image in the CSP; the section 6 defaults accepted)
  - [x] Workflow Planning — approved 2026-09-04 (A, as written): inception/plans/workflow-planning-post-unit10-followups.md; owner insertion the same day: a pinned site header as a quick change ahead of the planned PRs
  - [ ] Quick change (owner insertion 2026-09-04): pinned site header as PR 0 — requirements section 4.4; plan construction/plans/quick-pinned-header-plan.md; awaiting owner approval (orchestrator-implemented, five-area review and Copilot gate kept)
  - [ ] Unit 10 addendum (Phase 6): BJJ wording for the theme-toggle tooltip (feat, XS)
  - [ ] Unit 11: current belt for the rank bar (feat, S, migration AddCurrentBelt)
  - [ ] Unit 12: security headers and rate limiting (feat, M; PRs 12a headers, 12b rate limiting)

## Operations Phase — Post-Launch (2026-07-21 → present)
- [x] Deployed: live at https://joshuamykitta.dev (HTTP container behind reverse proxy, TLS at proxy; image ghcr.io/jjwren/portfolio)
- [x] OAuth apps registered; live sign-in + admin role verified in production
- [x] Production .env personalized; old JJWren/Personal_Portfolio repo archived
- [x] Release flow: squash-merged conventional-commit PRs + release-please tags/GHCR publish; Copilot review gate on every PR (see CONTRIBUTING.md)
- [x] Domain move (2026-07-24): portfolio.theguywiththedogs.dev → joshuamykitta.dev — Porkbun DNS repointed, new reverse-proxy host + Let's Encrypt cert, PUBLIC_BASE_URL swapped, OAuth callbacks re-registered (GitHub/Google/Discord), repo About homepage updated
- [x] Resolved 2026-07-29 as won't-do: the old-hostname 301 was superseded by an owner decision to retire portfolio.theguywiththedogs.dev outright (no known bookmarks, external links updated, DNS record slated for removal). Original intent — re-create the 301 redirect in the reverse proxy — see audit entry 2026-07-24T22:18:29Z; SEO research record in the LlmWiki (Portfolio-SEO-Improvements)
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
- Test suite: 41 → 184 → 250 test methods across 35 files at v1.22.0 (about 390 cases with theory rows; see inception/reverse-engineering/reverse-engineering-timestamp.md; unit-test-instructions.md still shows the older count)
- Full history in CHANGELOG.md (release-please); decision log in audit.md

## Key Decisions
- OAuth-only sign-in (no passwords); providers conditionally registered from env creds
- Admin = email in ADMIN_EMAILS env (comma-separated) → Admin role claim at sign-in
- Contact: SMTP send + DB copy, admin inbox
- Projects: DB-backed admin CRUD (image, title, homepage/repo, summary), carousel UI
- Theme: dark-first (#151515/#A63D40/#E9B872/#90A959/#6494AA), light toggle
- Self-hoster story: all personal details via env vars; image published to GHCR
