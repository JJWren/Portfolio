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

## Issue #22 Fix — Admin Projects Links + Visibility Toggle
**Timestamp**: 2026-07-22T21:40:00Z
**User Input**: "I see. They are visually smashing together to look like homerepo instead of looking like home repo or stacked home on top of repo, etc. I agree with the plan. I think while this page is being tweaked that the "visibile" badge should be clickable and switch to hidden or invisible and vice versa."
**AI Response**: Implemented on fix/admin-projects-links-badge from master: links cell renders home/repo as stacked mono-small anchors (mono-small removed from the td, fixing the block-display border break), Visible/Hidden badge became a toggle button backed by new ProjectService.ToggleVisibilityAsync, .badge-toggle CSS added. Research + plan recorded in LlmWiki decision Portfolio-Admin-Projects-Links-Fix (issue #22 root cause: mono-small display:block on td + unseparated inline spans).
**Context**: Brownfield bug fix, minimal depth — research done via LlmWiki research workflow; plan approved by user in chat

---

## Issue #25 Fix — Blog Post Length Validation
**Timestamp**: 2026-07-23T10:49:35Z
**User Input**: "on issue https://github.com/JJWren/Portfolio/issues/25. remediate finding. push changes and create fix pr. Wait for copilot's automated review (poll every 2min). review and remediate comments it leaves if necessary. push remediation. loop this process until copilot leave 0 comments. merge pr."
**AI Response**: Root cause: PostEditor enforced no field length limits while BlogPosts columns are capped (Slug 160, Title 200, Summary 500, HeaderImagePath 400) — an over-length summary reached SaveChanges and Postgres rejected the INSERT with 22001, crashing the save with an unhandled DbUpdateException. Added PostRules (single source for BlogPost column sizes + CheckLengths friendly validation), pointed AppDbContext and SlugHelper's 160 cap at the constants (values unchanged, no migration), added maxlength attributes + save-time guard + DbUpdateException fallback error to PostEditor, and PostRulesTests (118 tests green). Research recorded in LlmWiki decision Portfolio-Blog-Post-Length-Validation.
**Context**: Brownfield bug fix via research workflow; fix PR from fix/blog-post-length-validation, Copilot review gate before squash merge

---

## Issue #28 Fix — Project Length Validation
**Timestamp**: 2026-07-23T13:44:29Z
**User Input**: "do it. open the issue and then resolve it just as we did for the previous issue."
**AI Response**: Opened issue #28 (ProjectEditor same save-crash class as #25: Title 120 and both URLs 400 fully unguarded, Summary maxlength hardcoded client-side only, no server-side checks, no DbUpdateException fallback). Mirrored the #26 fix: ProjectRules single-sources the five Project column sizes + CheckLengths friendly validation, AppDbContext derives from the constants (values unchanged, no migration), ProjectEditor gains maxlength on title/summary/URLs + save-time guard on normalized values + DbUpdateException fallback error, ProjectRulesTests added (125 tests green). Research recorded in LlmWiki decision Portfolio-Blog-Post-Length-Validation (follow-up noted) and this fix's PR.
**Context**: Brownfield bug fix mirroring #26; fix PR from fix/project-length-validation, Copilot review gate before squash merge

---

## Issue #31 Fix — Header Image Constraints + Crop Tool
**Timestamp**: 2026-07-23T14:50:49Z
**User Input**: "plan a fix for the issue then remediate using the previously established pattern" (plan approved; design forks confirmed: bake crop into file client-side / one 16:9 box with hero-band guide / SVG+GIF bypass)
**AI Response**: ImageUploadService now exposes AcceptList, AllowedTypesDisplay, and MaxMegabytes (MaxBytes derived) so the editor hint and accept attributes single-source the real constraints. PostEditor header field gained the constraint hint, a crop entry input, and a 16:9 crop panel (zoom slider/wheel + drag pan, hero-band guide at 420/(1080*9/16) of box height); new wwwroot/js/crop.js (vanilla IIFE, site.js idiom) bakes the framed region onto a canvas (cap 1920x1080, source-format encode, PNG-over-cap JPEG fallback, SVG/GIF pass through) and feeds the hidden Blazor InputFile via DataTransfer so the existing upload path is unchanged. ImageUploadServiceTests added (128 tests green).
**Context**: Feature via approved plan; fix PR from feat/blog-header-image-crop, Copilot review gate before squash merge

---

## Issue #31 Scope Addition — Project Card Images
**Timestamp**: 2026-07-23T15:03:00Z
**User Input**: "these image changes should also apply similarly for the images that can be attached for projects."
**AI Response**: Extended PR #32 in place (crop.js was built prefix-parameterized for exactly this): ProjectEditor gained the same constraint hint, a project-prefixed crop panel, and the hidden InputFile hand-off wired via OnAfterRenderAsync init("project"). The project card renders 16:9 object-fit: cover as its only surface, so the box needs no guide band and crop.js required zero changes. 128 tests green.
**Context**: Scope addition by Joshua mid-review on feat/blog-header-image-crop; Copilot round covering it still gates the merge

---

## Issue #34 Fix — Crop Panel Visibility + Stored-Image Framing
**Timestamp**: 2026-07-23T20:53:54Z
**User Input**: "The images in both projects and blogs are not appearing in the crop section where I can actually edit them."
**AI Response**: Diagnosed empirically (standalone + Blazor-replica headless harnesses driving the real crop.js through DataTransfer→change→crop→hand-off): the fresh-pick pipeline works end to end; the actual defects were (1) the hidden attribute losing to .crop-panel display:grid (author rule beats UA [hidden] rule) so an empty panel rendered permanently in both editors, and (2) no path for stored images into the crop tool. Fix: class-toggle visibility (.crop-panel/.crop-panel.open), panel markup moved into the crop-available branch, new Adjust framing button fetching the stored same-origin image via __cropTool.open (promise-returning so failures surface as JSException), SVG/GIF excluded, ILogger on init failure. Replica beacon trail green: panel none at rest, opens on pick, closes on apply w/ upload reaching OnChange, open() loads stored image covering the box. 128 tests.
**Context**: Bug fix via plan-approved diagnose-first approach; fix PR from fix/crop-panel-image-display, Copilot gate before squash merge

---

## Issue #37 Fix — Crop Tool Never Appearing (Module Load)
**Timestamp**: 2026-07-23T22:20:00Z
**User Input**: "/remediate https://github.com/JJWren/Portfolio/issues/37" (issue: "Editing/Cropping images for blogs and projects not appearing when adding image" — expected crop modal with position/zoom after choosing an image; actual: nothing appears, the image simply attaches)
**AI Response**: Diagnosed from production evidence rather than harnesses this time: portfolio-web (1.9.1) container logs show JSException "Could not find '__cropTool.init' ('__cropTool' was undefined)" in PostEditor at 21:43:15Z — five seconds after the container started at 21:43:10Z — and in ProjectEditor at 21:44:39Z, same tab. The user's browser document predated the redeploy: crop.js never loaded in that document (asset fetch raced the container swap through nginx-proxy-manager while blazor.web.js/theme.js/app.css came from immutable cache), and a script tag only executes at document birth, so every circuit that tab ever hosts finds __cropTool undefined and silently degrades both editors to the plain uploader (issue's screenshots confirm: fallback InputFile, no Adjust framing button, .jpeg/.png picks attaching directly). Fix: crop.js converted to an ES module (export init/open, IIFE and window global removed, logic untouched); both editors import it per circuit in OnAfterRenderAsync via JS interop import with Assets["js/crop.js"] (fingerprint resolved server-side at render time, so even a stale tab fetches the current build's module from the live server), hold the IJSObjectReference, dispose it in DisposeAsync (JSDisconnectedException swallowed), and show a visible "Crop tool couldn't load" hint in fallback mode instead of degrading silently. App.razor script tag removed. Headless-Edge harness beacon green: no global, exports present, re-init guard holds, panel opens on pick, Apply hands off cropped PNG to the Blazor target, panel closes. Build warning-clean, 128 tests green.
**Context**: Bug fix via /remediate; fix PR from fix/crop-tool-module-load, Copilot gate before squash merge

---

## Issue #40 Fix — Crop Tool Ground-Up Rework (Crop-Box Paradigm)
**Timestamp**: 2026-07-24T03:01:34Z
**User Input**: "/work-a-github-issue https://github.com/JJWren/Portfolio/issues/40" (issue: crop tool not firing at all — both editors show "Crop tool couldn't load — images upload as-is. Reload the page to retry."; requested scrapping the current tool and reworking from the ground up: local canvas rendering, movable/resizable crop box, live adjustment showing exactly what the blog/project will display, cropped result converted and uploaded). Design forks answered via question UI: "Locked 16:9 (Recommended)"; "Shared component (Recommended)"; "Keep zoom slider + wheel"; "I want to see a design mock-up of what it will look like"; after interactive mock-up delivered: "Approve — implement now".
**AI Response**: Root cause of the permanent load failure found in the .NET 10 JSInterop source: Blazor's interop `import` only rebases URLs starting with "./" onto the document base href; the #38 fix passed the bare fingerprinted path from Assets["js/crop.js"], which native import() rejects as a bare module specifier before any request — so every circuit landed in the fallback deterministically. Fix + rework: new JsModuleUrl.Resolve prefixes bare asset paths with "./" (JsModuleUrlTests cover the regression); crop.js rewritten around the issue's paradigm — image rendered in a stage with a movable, corner-resizable crop box locked to 16:9, zoom slider/wheel toward focus with pan when zoomed, rule-of-thirds guides, hero-band guide (posts), live "Blog card and list"/"Post hero — desktop" (and "Project card") canvas previews, and a selection→saves readout; Apply extracts the boxed region on a canvas (cap 1920×1080, never upscaled, source-format encode with PNG-over-cap JPEG fallback) and hands the File to the hidden Blazor InputFile so ImageUploadService still validates/stores; SVG/GIF pass-through, Use full image, Cancel, stored-image open() for Adjust framing, and AbortController re-init hygiene all kept. Duplicated field markup + interop lifecycle (~100 lines per editor) extracted into shared ImageCropField.razor (per-circuit import, plain-uploader fallback, inline upload errors); PostEditor/ProjectEditor now bind one component tag each; app.css crop styles reworked (stage, frame, handles, dim, previews) on existing tokens. Interactive design mock-up (site fonts/palette, generated sample image) approved before coding. Validation: build warning-clean, 133 tests green (128 + 5 new), headless-Edge harness against the real module 15/15 — import, init, pick→open, fit geometry, zoom, aspect-locked resize, drag clamp, readout floor-math, preview pixels, Apply hand-off name/type, exact output dims (237×133), extracted-region pixels, reset, stored-image reopen, SVG pass-through.
**Context**: Ground-up rework via /work-a-github-issue with design-mock approval gate; fix PR #41 from fix/crop-tool-rework, Copilot gate before squash merge

---

## Docs Refresh — README Features, AIDLC State, Repo About Metadata
**Timestamp**: 2026-07-24T16:29:59Z
**User Input**: "https://github.com/JJWren/Portfolio Do a docs update on the aidlc-docs to ensure that READMEs and other related documentation for the project are appropriately updated with critical features, etc. Also, ensure that the github about section and tags are appropriately up to date." (via /github-projects-readonly)
**AI Response**: README Features/Configuration brought up to the v1.12.0 feature set (comment profiles + anonymous posting; reporting/moderation/bans/user messages; blog search/month/tag filters + pagination; 16:9 crop-box image editor; admin notification badges + sortable/filterable tables; /admin/site landing-content overrides). aidlc-state.md gained an Operations Phase — Post-Launch section (deploy verified live at portfolio.theguywiththedogs.dev, OAuth live, Personal_Portfolio archived, v1.1.0→v1.12.0 release summary) and the stale Unit 1/Unit 3 pending notes were resolved. build-and-test-summary.md gained a post-hand-off update marking user items 1–4 complete. unit-test-instructions.md coverage refreshed: 41 → 184 tests across 23 fixtures. Repo About updated via gh: topics added (repo previously had none) and homepage set to the live URL; description already accurate and kept. GitHub Projects boards were unreadable (available tokens lack read:project) — feature status sourced from CHANGELOG.md, source, and the live site.
**Context**: Documentation-only update; docs PR from docs/readme-aidlc-refresh, Copilot review gate before squash merge

---

## Operations — Domain Move to joshuamykitta.dev
**Timestamp**: 2026-07-24T22:18:29Z
**User Input**: "Review my information here (Z:\docker\portfolio) and here (https://github.com/JJWren/Portfolio) and tell me what I need to do to transition my portfolio to a new domain (joshuamykitta.dev)" → "Give me very detailed steps for this checklist. Don't gloss over details. Treat me like an idiot doing this for the first time." → "Go ahead and do the docs PR. The website is live and working."
**AI Response**: Mapped every domain touchpoint (Porkbun DNS, NPM proxy host + Let's Encrypt cert npm-34, PUBLIC_BASE_URL and OAuth callback registrations in .env, GitHub/Google/Discord dashboards, repo About, external profile links) and confirmed the app itself is host-agnostic (AllowedHosts *, forwarded headers configured, all domain values env-driven) — zero code changes needed. Delivered a detailed first-timer cutover guide: Porkbun A/CNAME records → new NPM proxy host + HTTP-01 cert with websockets on → PUBLIC_BASE_URL swap + container recreate → OAuth callback updates (GitHub is a hard single-callback switch; Google/Discord additive) → old host converted to a 301 redirection host reusing cert npm-34 → repo About + external links. Joshua executed the cutover; verified live: GET https://joshuamykitta.dev returns 200, repo About homepage already updated. Docs refreshed in this PR: aidlc-state.md (deployed URL + domain-move bullet), build-and-test-summary.md (PUBLIC_BASE_URL example + deployed-at URL). Flagged during verification: the old hostname currently serves no NPM host at all (proxy host deleted, redirection host not yet created — HTTPS handshake fails, HTTP 404s), so the planned permanent 301 for legacy links still needs to be configured in NPM.
**Context**: Operations phase, domain cutover; docs-only PR from docs/domain-move-joshuamykitta, Copilot review gate before squash merge; docs: commit → no release

---

## Feature — Markdown-Compatible Comments & Messages (Issue #56)
**Timestamp**: 2026-07-25T20:35:00Z
**User Input**: "/research-workflow https://github.com/JJWren/Portfolio I want comments and messages to be markdown compatible." → "Include the admin contact inbox and then proceed with the github issue and the pr-with-copilot gate flow"
**AI Response**: Three-phase research (wiki decision Portfolio-Comments-Messages-Markdown) determined render-time markdown for user-generated text via a hardened path split from the trusted blog pipeline (whose UseAdvancedExtensions includes GenericAttributes — arbitrary HTML attributes — plus raw-HTML passthrough, so it must never see visitor input). Implementation: MarkdownService.ToSafeHtml — restricted pipeline (DisableHtml; autolinks, pipe tables, strikethrough, softline→hardline) with a DocumentProcessed AST guard stripping images and non-http/https/mailto link schemes (CommonMark treats javascript: destinations as legal) and tagging surviving links rel="nofollow ugc noopener"; ToPlainText for one-line excerpts. Four surfaces render through it: CommentSection, /messages inbox, MyReports responses, admin contact inbox. QuotedComment snapshots, the CommentsAdmin moderation cell, and report details stay plain-encoded for source fidelity. Comment form gains a markdown hint + preview toggle (PostEditor pattern); .prose gets a compact .prose-sm modifier. The planned HtmlSanitizer 9.0.967 dependency was dropped mid-implementation: NuGet audit failed the -warnaserror build — its latest stable exact-pins AngleSharp [0.17.1], vulnerable to CVE-2026-54570 (mXSS; the advisory warns sanitizers that trust AngleSharp may be bypassable), and the patched line is beta-only — replaced by the zero-dependency AST-level guard. Build warning-clean; 255/255 tests (+16 covering XSS vectors: raw HTML, event-handler injection, generic-attribute syntax, javascript: links/autolinks, image stripping; plus feature/flattening cases).
**Context**: Construction, feature PR from feat/markdown-comments-messages, Copilot review gate before squash merge; Closes #56

---

## Feature — Live Discord-Style Markdown Composer (Issue #59)
**Timestamp**: 2026-07-25T22:30:00Z
**User Input**: "Enhance the previous request by having the markdown recognized while the user is typing and so things like common languages are picked up for code blocks (similar to discord's markdown). Create the enhancement issue. Research. Work the item per the PR copilot loop."
**AI Response**: Research (wiki decision Portfolio-Live-Markdown-Editor) selected an overlay-mirror composer over contenteditable/CodeMirror/side-by-side alternatives: shared MarkdownInput.razor stacks a styled aria-hidden mirror beneath a glyph-transparent textarea, with vendored md-input.js (escape-first display tokenizer: emphasis/strike/inline code/links/bare URLs/quotes/headings/list markers; fenced bodies live-highlighted via Prism.highlight whenever Prism.languages knows the tag — the vendored bundle's js/ts/cs/py/sh/yml/html/css/sql/json set). Decisive research finding: overlay caret-accuracy requires identical glyph metrics in both layers, forcing the composer to --font-mono (JetBrains Mono advance widths are fixed under real or faux bold/italic; proportional Public Sans is not) — a deliberate, visible font change. Adopted by the comment box and both ReportsAdmin composers (their hardcoded maxlength="2000" literals retired for CommentRules.MaxLength). site.js MutationObserver extended to Prism-highlight code blocks rendered by InteractiveServer islands (comments/inboxes/preview never highlighted before — only static-SSR load and enhancedload had hooks). Module lifecycle mirrors crop.js (per-circuit import via JsModuleUrl, silent plain-textarea fallback, IAsyncDisposable, AbortController re-init hygiene). Zero new dependencies; no server pipeline changes; build -warnaserror clean; 258/258 tests; headless-Edge harness 22/22 against the real module (token cases, Prism pickup/alias/unknown fallback, escape-first rendering, scroll sync, metric parity incl. trailing-newline sentinel, refresh/dispose/re-init).
**Context**: Construction, feature PR from feat/live-markdown-input, Copilot review gate before squash merge; Closes #59

---

## Feature — SEO: Canonical URLs, Social Cards, Structured Data (Research Workflow P1)
**Timestamp**: 2026-07-29T14:20:00Z
**User Input**: "/research-workflow https://github.com/JJWren/Portfolio Check this repo and see if there are ways to increase SEO." → "walk me through my parts with lots of detail. I already added the site to google search console and verified." → "Part 1 is complete with the following caveats: - the redirect in the browser is taking me to my typo https://joshuamykittta.dev/ instead of https://joshuamykitta.dev/ . I already fixed this in NPM. Curl is seeing the correct redirect link. This seems isolated to my browser for some reason. - Nobody as the portfolio website bookmarker. All links on socials have been updated. I am not redirecting. I am killing that off. Part 2 is complete Part 3 is complete Let's discuss Part 4" → AskUserQuestion decisions: decouple the search snippet via SITE_META_DESCRIPTION (hero keeps the tagline), SITE_TITLE → "Joshua Mykitta — Software Engineer" (owner env edit), and start the P1 PR now.
**AI Response**: Three-phase research (wiki decision Portfolio-SEO-Improvements) found a strong on-page baseline (static-SSR head, robots/sitemap/RSS on PUBLIC_BASE_URL, per-page titles, UGC nofollow, real 404s) but: the domain not yet indexed (site:/exact-title probes empty five days post-move), every page serving at four-plus uncanonicalized spellings (http 200, https://www 200, trailing slashes, /blog filter params; zero rel=canonical/og:url), a live social-card bug (relative og:image on posts violates the OG spec), no twitter:card/default image/JSON-LD, and three pages without descriptions. Owner executed P0 same day: Search Console registered+verified, sitemap submitted, 7 indexing requests, Bing import, NPM Force SSL + www→apex 301 (wire-verified), GitHub/LinkedIn profile links; the old-hostname 301 was cancelled by owner decision (domain retired outright; aidlc-state item closed as won't-do). This PR is the code half: SeoRules (canonical origin/URL normalization, 160-char word-boundary description truncation, serializer-escaped Person/WebSite/BlogPosting JSON-LD — MarkupString-safe by construction), SocialMeta shared head component (description + og:title/description/type + absolute og:image with a neutral ribbon-card fallback at wwwroot/social-card.png + article times; JSON-LD rides as ChildContent because the framework keeps only the last-rendered HeadContent), App.razor canonical/og:url/twitter:card on every page (query dropped so filter/pager variants self-canonicalize), SITE_META_DESCRIPTION env (SiteConfig + .env.example + README; homepage falls back to the effective tagline), SeoEndpoints.BaseUrl re-rooted onto SeoRules.CanonicalOrigin. Build -warnaserror clean; 291/291 tests (+33: SeoRules suite + SiteConfig meta-description cases).
**Context**: Construction, feature PR from feat/seo-metadata, Copilot review gate before squash merge

---

## Feature — Hygiene: HEAD Support, Upload Caching, Soft-404, Headings, PWA Icons (SEO P2)
**Timestamp**: 2026-07-29T16:40:00Z
**User Input**: "explain to me what these are" → "Go ahead an work on these using the standard loop process"
**AI Response**: The five P2 hygiene items from Portfolio-SEO-Improvements, one PR: (1) /uploads now sends Cache-Control public,max-age=31536000,immutable (GUID filenames are single-use, content immutable; previously ETag/Last-Modified only). (2) HEAD requests no longer 405 — a HEAD-as-GET middleware discards the body per RFC 9110; smoke testing exposed that WebApplication's implicit UseRouting runs before all user middleware, so endpoint selection saw HEAD before the rewrite (405 → re-execute → 404 regression) — fixed with an explicit app.UseRouting() after the rewrite, the documented placement for UseStatusCodePagesWithReExecute anyway. (3) Direct GET /not-found returned 200 thin content (soft-404); NotFound.razor now stamps 404 on direct visits only, using IStatusCodeReExecuteFeature to leave re-executed statuses (404/405) untouched. (4) Listing cards jumped h1→h3; /blog and /projects cards are now h2 with selectors retargeted (admin .card h3 untouched). (5) apple-touch-icon (180) + favicon-192/512 generated from logo.png centered on #151515, site.webmanifest served by a new endpoint (SeoRules.WebManifest — name/short_name from SiteConfig, colors from the effective ThemeService snapshot, so admin theme overrides flow into the manifest) with head links in App.razor. Build -warnaserror clean; 297/297 tests (+2 manifest). Compose smoke matrix: HEAD 200/200/200 on pages+feed and 404 on garbage, direct /not-found 404, re-execute preserved, uploads header verified via exec-dropped file, manifest application/manifest+json with theme color, icons 200 image/png, h2 cards, home head regression-free.
**Context**: Construction, feature PR from feat/seo-hygiene, Copilot review gate before squash merge

---

## Feature — Admin-Supplied Alt Text for Header/Card Images (SEO backlog close-out)
**Timestamp**: 2026-07-30T02:05:00Z
**User Input**: "Would the adjustment for that alt-text also allow me to put that alt-text somewhere (such as when creating/editing blogs/projects)?" → "go for it"
**AI Response**: The last open SEO-backlog item: nullable HeaderImageAlt (varchar 200) on BlogPost + Project (migration AddHeaderImageAlt, auto-applied), caps single-sourced as PostRules/ProjectRules.HeaderImageAltMaxLength with CheckLengths coverage, "Image description" inputs under the ImageCropField in both editors (trim-to-null on save, maxlength, friendly over-limit errors), and render wiring: post hero alt (coalesced — Blazor omits null-valued attributes and a content image without alt is worse than an empty one), og:image:alt via a new SocialMeta ImageAlt parameter (emitted only when set), project card alt. Blog list thumbnails deliberately stay alt=""/aria-hidden — decorative next to the linked title. Build -warnaserror clean; 299/299 tests (+2). Compose smoke: migration applied on startup, psql-injected image+alt rendered on the post hero and project card, og:image:alt present with alt and absent without, home head regression-free.
**Context**: Construction, feature PR from feat/header-image-alt, Copilot review gate before squash merge

---

## Feature — Contact-Form Spam Hardening + First-Party Privacy-Preserving Analytics
**Timestamp**: 2026-08-14T17:45:00Z
**User Input**: "/mattpocock-skills:grill-with-docs what can be implemented into the portfolio project to reduce spam in the contact form and fake emails, etc? Also, can we track visitors and actual engagement but not record any personal/sensitive data?" → Round 1 answers: "This all looks good. Yes, the privacy statement would need a minor adjustment per your recommendation." → Round 2 answers: "Looks good" → Plan approved via plan mode.
**AI Response**: Grilling session settled the design tree (bots blocked mechanically, human sales mail quarantined not blocked; fully self-contained, no CAPTCHA; fake emails = MX-fail + disposable domains; confidence-split disposition; first-party server-side cookieless analytics; daily-rotating hashed visitor keys; DNT/GPC honored; 90-day raw retention → permanent daily aggregates). Four increments delivered on feat/spam-hardening-and-analytics: (1) Spam — ContactSpamRules (4s min-submit, link/subject-URL heuristics), ContactFormTimestamp (DataProtection-protected render token; undecipherable token quarantines rather than drops, surviving key rotation), DisposableEmailDomains (vendored 8.2k-domain embedded blocklist, parent-suffix matching), MailDomainChecker (DnsClient MX→A/AAAA, 2.5s cap, fail-open), ContactMessage.IsFlagged/FlagReason (migration AddContactMessageFlagging), flagged messages skip SMTP + excluded from unread badge, Messages.razor Flagged filter + "Not spam" action, Dashboard flagged badge. (2) Analytics capture — PageView/AnalyticsEvent/AnalyticsState entities (migration AddAnalyticsCapture), VisitorKey (HMAC-SHA256 of per-install secret + UTC date + IP + UA), AnalyticsRules (path/bot/DNT/referrer rules), AnalyticsMiddleware after auth (200-HTML-only, HEAD-rewrite + status-re-execute guards, Admin sessions excluded), /go/{id}/{kind} redirect endpoint (DB-sourced URLs only, no open redirect) + RESUME_FILE-gated /resume endpoint, project links rerouted with data-enhance-nav="false". (3) Rollup — DailySiteStat (Day PK = watermark)/DailyRouteStat/DailyReferrerStat/DailyEventStat (migration AddAnalyticsRollups), AnalyticsRollup pure aggregation + NextRunUtc(00:20 UTC), AnalyticsRollupService — the app's first BackgroundService (startup catch-up, idempotent delete-then-insert per day, 90-day ExecuteDelete retention, FakeTimeProvider-drivable Task.Delay). (4) /admin/stats (period select, five stat tiles, sortable paged top-pages, referrers, events; "daily visitors" labeled as such — cross-day uniques intentionally impossible), Dashboard Stats card, Privacy.razor reworded honestly (server-side aggregate stats, hash mechanics, 90-day deletion, DNT/GPC), README/.env.example RESUME_FILE, CONTEXT.md glossary seeded, ADR 0001 (first-party cookieless analytics). Build clean; 390/390 tests (+48 across 8 new/extended pure-logic suites).
**Context**: Construction, feature branch feat/spam-hardening-and-analytics, PR pending

**Addendum (live smoke test)**: Ran the app against a throwaway Postgres and exercised every path with curl. Two real bugs caught and fixed: (1) DI constructed DisposableEmailDomains via its IEnumerable<string> test constructor (MS.DI resolves IEnumerable<T> as "all registered T" = empty) → empty blocklist in production; fixed with an explicit factory registration. (2) DateOnly.ToDateTime(TimeOnly.MinValue) yields Kind=Unspecified, which Npgsql rejects against timestamptz → rollup crashed and /admin/stats "today" queries would throw; fixed with the DateTimeKind.Utc overload (6 sites). Verified live after fixes: honeypot + sub-4s submits silently dropped (no row), disposable-domain and body-links quarantined (flagged, no SMTP), rate limiter intact, bot-UA/DNT/HEAD/404 requests not counted, referrer normalized, /go 302 + project-click event, /resume 404 when unset, rollup catch-up aggregated a back-dated day + zero-traffic watermark day correctly with raw rows retained. MX NoMailServer path not verifiable on the workstation LAN (port-53 to external resolvers blocked → fail-open Unknown, by design); logic unit-tested.
