# Audit Trail — Portfolio Rebuild

## Initial User Request
**Timestamp**: 2026-07-20T00:00:00Z
**User Input**: "I want to completely redo my personal portfolio page (https://github.com/JJWren/Personal_Portfolio). I want to move away from pure html/css/js and make things more modern and standardized. I still want to be able to utilize a contact form. I no longer want to host my resume (in html or via file) and I would rather just direct visitors to my linkedin (https://www.linkedin.com/in/joshuamykitta/). I think I would like to add a blog section that uses a backend database for CRUDing posts in and also allowing users to comment on blog posts. This means some sort of OAuth should be used and there would need to be hidden admin settings only available to admin users (me being a default admin as the owner of the website). I want it dockerized (compose). I would like it to be developed in a way that someone that wants to use it could spin up a docker container of it with some .env variables set in the background that would establish contact details (linkedin, name, phone, email, etc) and project information (project links with header image, etc). I want a projects page that contains a link to my github landing page (.env variable) with a list of projects as cards on a carousel (image, title, homepage | repo, short summary description). Help me plan this and think of other potential things for this portfolio. I want the theme to be dark mode first and using this color set (https://coolors.co/151515-a63d40-e9b872-90a959-6494aa)"
**AI Response**: Entered Inception Phase (greenfield — complete redo; existing repo is content reference only). Fetched existing repo overview, then asked clarifying questions (multiple choice).
**Context**: Workspace Detection + Requirements Analysis

---

## Requirements Clarification — Round 1
**Timestamp**: 2026-07-20T00:00:00Z
**User Input**: Tech stack: "Option 1, but let's pull down the latest .NET Core SDK which should be at least .NET 10." | Database: "PostgreSQL (Recommended)" | OAuth providers: "GitHub (Recommended), Google, Discord" | Contact form: "SMTP email + DB copy (Recommended)"
**AI Response**: Recorded decisions: ASP.NET Core + Blazor on .NET 10, PostgreSQL, all three OAuth providers, SMTP + DB contact handling.
**Context**: Requirements Analysis — clarifying questions

---

## Requirements Clarification — Round 2
**Timestamp**: 2026-07-20T00:00:00Z
**User Input**: Projects data: "Admin-managed in DB with a form to post new projects (image, title, links {homepage and/or repo}, short summary description). This means we also need to be able to edit or remove projects." | Theming: "Dark-first with light toggle (Recommended)" | Repo: "New repo (Recommended)"
**AI Response**: Recorded decisions: admin CRUD for projects (create/edit/delete with image upload), dark-first theme with light toggle, new repository for the codebase.
**Context**: Requirements Analysis — clarifying questions

---

## Workflow Plan Approval
**Timestamp**: 2026-07-20T00:00:00Z
**User Input**: Approved plan via plan-mode approval ("User has approved your plan.")
**AI Response**: Plan saved (Blazor .NET 10 + PostgreSQL + Docker Compose; 8 units of work: scaffold/docker, theming/layout, identity/OAuth, blog, comments, projects, contact, extras/CI). Began Construction Phase Unit 1.
**Context**: Workflow Planning — approval; transition to Construction Phase

---

## Construction — Unit 1: Scaffold + Docker skeleton
**Timestamp**: 2026-07-20T23:10:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: Installed .NET SDK 10.0.302; scaffolded Portfolio.slnx with Blazor Web App (src/Portfolio.Web) and xUnit tests (tests/Portfolio.Tests); added Npgsql.EFCore.PostgreSQL, Identity.EFCore, EFCore.Design, Markdig; created AppDbContext (IdentityDbContext<ApplicationUser>) with InitialIdentity migration applied on startup; wrote Dockerfile, docker-compose.yml (web + postgres:17-alpine), .dockerignore, .env.example, README. Verified: dotnet build/test pass; docker compose up serves HTTP 200 with migrations applied. Fixes along the way: solution file is .slnx (new .NET 10 default) — Dockerfile updated; installed libgssapi-krb5-2 in runtime image (Npgsql GSSAPI probe error); persisted data-protection keys to dpkeys volume; HTTPS redirect gated to development.
**Context**: Unit 1 code generation complete; presenting 2-option checkpoint

---

## Unit 1 Checkpoint Approval
**Timestamp**: 2026-07-20T23:20:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 1 approved. Beginning Unit 2: Theming, layout, static pages.
**Context**: Construction — Unit 1 → Unit 2 transition

---

## Construction — Unit 2: Theming, layout, static pages
**Timestamp**: 2026-07-20T23:55:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: Bundled Fraunces/Public Sans/JetBrains Mono woff2 fonts locally; replaced Bootstrap+template CSS with a custom token system (dark-first palette #151515/#A63D40/#E9B872/#90A959/#6494AA, light overrides on warm paper); pre-paint theme script + toggle (localStorage, prefers-color-scheme hint, dark default); four-color ribbon signature (brand mark, hero ribbons, section ticks, footer strip); SiteConfig service bound from env with startup validation (+SITE_ABOUT, SITE_SKILLS added to .env.example); new MainLayout (top nav + footer) and Home (hero + about + skills). Removed Counter/Weather/NavMenu/Bootstrap. Verified: build + 5 tests green, compose serves new UI, all font/css/js assets 200. Visual screenshot review skipped (browser tools unavailable) — user to review at localhost:8080.
**Context**: Unit 2 code generation complete; presenting 2-option checkpoint

---

## Unit 2 Checkpoint Approval
**Timestamp**: 2026-07-21T00:05:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 2 approved. Beginning Unit 3: Identity + OAuth (GitHub/Google/Discord).
**Context**: Construction — Unit 2 → Unit 3 transition

---

## Construction — Unit 3: Identity + OAuth
**Timestamp**: 2026-07-21T00:40:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: External-login-only Identity (no passwords): AddIdentityCore + roles + Identity cookies; GitHub/Google/Discord registered only when OAUTH__{P}__CLIENTID/SECRET present; /auth/login/{provider}, /auth/external-callback (create-or-link user by email, refresh display name, sync Admin role from ADMIN_EMAILS every sign-in, open-redirect guard), POST /auth/logout with antiforgery; Admin role seeded at startup; forwarded headers for reverse proxy; /signin provider picker; nav auth controls; role-gated /admin dashboard shell; unauthorized renders as 404 (hidden admin). Verified: build + 15 tests green; /signin 200; anon /admin → 302 to /signin; no Admin-link leak. Live OAuth round-trip requires user-registered OAuth apps.
**Context**: Unit 3 code generation complete; presenting 2-option checkpoint

---

## Unit 3 Checkpoint Approval
**Timestamp**: 2026-07-21T00:45:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 3 approved. Beginning Unit 4: Blog (entities, admin CRUD, public pages).
**Context**: Construction — Unit 3 → Unit 4 transition

---

## Construction — Unit 4: Blog
**Timestamp**: 2026-07-21T01:30:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: BlogPost entity + AddBlogPosts migration (unique slug, tags as text[]); MarkdownService (Markdig advanced + reading time), SlugHelper, BlogService (factory-based EF), ImageUploadService (/uploads volume + static file mapping); public /blog and /blog/{slug} (404 for bad slugs); admin /admin/posts list and InteractiveServer PostEditor (live preview, slug auto-suggest, tags, header-image upload, publish toggle, delete confirm); Prism bundled locally (11 languages) with palette-matched theme + enhanced-nav re-highlight. Verified via compose: seeded post renders in list and detail (bold, code fence class, tags), bad slug 404s; 32 tests green. Seed post 'hello-world' left in dev DB for user testing.
**Context**: Unit 4 code generation complete; presenting 2-option checkpoint

---

## Unit 4 Checkpoint Approval
**Timestamp**: 2026-07-21T01:35:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 4 approved. Beginning Unit 5: Comments + moderation.
**Context**: Construction — Unit 4 → Unit 5 transition

---

## Construction — Unit 5: Comments + moderation
**Timestamp**: 2026-07-21T02:00:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: Comment entity (cascade FKs, ≤2000-char plain-text body, IsHidden soft-hide) + AddComments migration; CommentRules validation + CommentService; InteractiveServer CommentSection on post pages (list, post form for signed-in users, sign-in prompt with returnUrl, own-delete + admin inline delete); /admin/comments moderation table with hide/unhide/delete; dashboard link. Comments render encoded (no markdown/XSS surface). Verified via compose with seeded user+comment: renders publicly, hide toggle removes it, empty state appears; 38 tests green.
**Context**: Unit 5 code generation complete; presenting 2-option checkpoint

---

## Unit 5 Checkpoint Approval
**Timestamp**: 2026-07-21T02:05:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 5 approved. Beginning Unit 6: Projects (admin CRUD + carousel).
**Context**: Construction — Unit 5 → Unit 6 transition

---

## Construction — Unit 6: Projects
**Timestamp**: 2026-07-21T02:30:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: Project entity + AddProjects migration; ProjectService (sort-order management with neighbor-swap moves); public /projects scroll-snap carousel (prev/next JS scroll, 16:9 card images with ribbon-gradient fallback, homepage|repo links, GITHUB_URL CTA); admin /admin/projects (reorder arrows, visibility) + ProjectEditor (upload, visible toggle, delete confirm); dashboard link. Verified via compose with 3 seeded projects: visible cards render with links, hidden project excluded, fallback + CTA + controls present; 38 tests green.
**Context**: Unit 6 code generation complete; presenting 2-option checkpoint

---

## Unit 6 Checkpoint Approval
**Timestamp**: 2026-07-21T02:35:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 6 approved. Beginning Unit 7: Contact form (SMTP + DB + admin inbox).
**Context**: Construction — Unit 6 → Unit 7 transition

---

## Construction — Unit 7: Contact form
**Timestamp**: 2026-07-21T03:10:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: ContactMessage entity + migration; MailKit EmailService (env SMTP, no-op without SMTP__HOST, DB save always precedes best-effort email, replyTo visitor); ContactRateLimiter (3 per 10 min per IP); static-SSR /contact form with DataAnnotations validation, honeypot (silent fake success), rate-limit messaging, success state, contact-details aside; /admin/messages inbox (unread badges, expand, mark read/unread, mailto reply, delete). Verified via compose: genuine POST with antiforgery token stored + success shown; honeypot POST stored nothing. 41 tests green.
**Context**: Unit 7 code generation complete; presenting 2-option checkpoint

---

## Unit 7 Checkpoint Approval
**Timestamp**: 2026-07-21T03:15:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 7 approved. Beginning Unit 8: Extras + polish + CI.
**Context**: Construction — Unit 7 → Unit 8 transition

---

## Construction — Unit 8: Extras + polish + CI
**Timestamp**: 2026-07-21T03:55:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: RSS feed (/feed.xml), sitemap.xml, robots.txt (admin/auth disallowed) with PUBLIC_BASE_URL support; OpenGraph meta (site-wide + per-post article/og:image) and RSS alternate link; SEED_DEMO_DATA startup seeder; /healthz with DB check + curl-based compose healthcheck; GitHub Actions CI (build/test on push+PR, GHCR publish latest on master and semver on v* tags); full self-hosting README. Verified clean-slate: down -v → up --build reran migrations, seeded demo content, all endpoints 200, both containers healthy. Created public repo JJWren/Portfolio, initial commit of 97 files, pushed; first CI run started.
**Context**: Unit 8 code generation complete; presenting 2-option checkpoint

---

## Unit 8 Checkpoint Approval
**Timestamp**: 2026-07-21T04:00:00Z
**User Input**: "B) Continue to Next Stage"
**AI Response**: Unit 8 approved. Beginning Build and Test stage (instruction docs + closing verification).
**Context**: Construction — Unit 8 → Build and Test transition

---

## Construction — Build and Test
**Timestamp**: 2026-07-21T04:15:00Z
**User Input**: (none — autonomous execution of approved plan)
**AI Response**: Generated build-and-test instruction docs (build, unit-test, integration-test, performance-test, summary) in aidlc-docs/construction/build-and-test/; committed and pushed. First GitHub Actions CI run (build + 41 tests + GHCR image publish) completed with SUCCESS — ghcr.io/jjwren/portfolio:latest published. Outstanding user-only items documented in build-and-test-summary.md (OAuth apps, SMTP creds, .env personalization, deployment, archive old repo).
**Context**: Build and Test complete; asking final stage-gate question

---

## Workflow Close-out
**Timestamp**: 2026-07-21T04:25:00Z
**User Input**: "Yes — close out the workflow"
**AI Response**: Workflow closed. Operations stage remains a placeholder; deployment (home lab reverse proxy), OAuth app registration, SMTP creds, and .env personalization are user follow-ups documented in build-and-test-summary.md and README.
**Context**: AIDLC workflow complete — all Inception and Construction stages done

---
