# Code Structure

Snapshot at `master` @ `e1825d9` (v1.22.0). Build output (`bin/`, `obj/`) omitted.

## Repository layout

```text
Portfolio/
├── Portfolio.slnx                  # XML solution: one web project, one test project
├── global.json                     # SDK 10.0.302, rollForward latestFeature
├── dotnet-tools.json               # dotnet-ef 10.0.10
├── Dockerfile                      # multi-stage build; runtime listens on :8080
├── docker-compose.yml              # web + postgres:17-alpine; pgdata/uploads/dpkeys volumes
├── .env.example                    # every personalization and secret knob, documented
├── README.md                       # self-hoster manual (features, env table, OAuth callbacks, ops)
├── CONTEXT.md                      # ubiquitous-language glossary (Owner Photo, Hard Signal, Visitor Key, ...)
├── CONTRIBUTING.md                 # squash-merged conventional-commit PRs; Copilot review gate
├── CHANGELOG.md                    # release-please generated, v1.0.0 (2026-07-21) to v1.22.0 (2026-08-15)
├── LICENSE                         # MIT
├── .github/workflows/
│   ├── ci.yml                      # restore, build -warnaserror, test; PR-title check
│   └── release-please.yml          # version PRs/tags; GHCR publish on release creation
├── docs/adr/
│   └── 0001-first-party-cookieless-analytics.md
├── aidlc-docs/                     # AI-DLC workflow artifacts (state, audit trail, plans, this folder)
├── src/Portfolio.Web/
│   ├── Program.cs                  # DI registrations (all singletons), auth, pipeline, endpoints
│   ├── Portfolio.Web.csproj        # net10.0; 11 package references; embedded blocklist resource
│   ├── appsettings*.json           # logging levels only (dev: localhost connection string)
│   ├── Components/
│   │   ├── App.razor               # <head> assembly: theme script, app.css, admin override <style>, SEO meta
│   │   ├── Routes.razor            # Router + AuthorizeRouteView; NotAuthorized renders the 404 body
│   │   ├── _Imports.razor
│   │   ├── LandingSections.razor   # hero + about/skills; shared by Home and the admin theme preview
│   │   ├── SocialMeta.razor        # description, og:*, twitter:card, article times, JSON-LD child content
│   │   ├── Icon.razor              # inline SVG glyphs switched by IconKind
│   │   ├── CommentSection.razor    # InteractiveServer island on post pages
│   │   ├── MarkdownInput.razor     # live markdown composer (overlay mirror + md-input.js)
│   │   ├── NotFoundContent.razor   # shared 404 body
│   │   ├── Pager.razor             # link-based pager for static SSR lists
│   │   ├── PagerControls.razor     # callback pager for interactive admin tables
│   │   ├── SortHeader.razor        # sortable table header
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor    # header/nav/theme toggle, <main>, footer, blazor-error-ui
│   │   │   ├── ReconnectModal.razor (+ .razor.css, .razor.js)
│   │   ├── Pages/                  # public + account pages (13 files, 13 routes)
│   │   └── Admin/                  # admin pages (12 files, 13 routes) incl. ImageCropField
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs        # /auth/login/{provider}, /auth/external-callback, POST /auth/logout
│   │   ├── SeoEndpoints.cs         # /feed.xml, /owner-photo, /sitemap.xml, /robots.txt, /site.webmanifest
│   │   └── AnalyticsEndpoints.cs   # /go/{id}/{kind}, /resume
│   ├── Services/                   # 48 files: *Rules (pure), *Service (DB/IO), helpers
│   ├── Data/                       # AppDbContext + 16 entity classes
│   ├── Migrations/                 # 18 EF migrations + model snapshot (2026-07-20 to 2026-08-15)
│   ├── Resources/
│   │   └── disposable-email-domains.txt   # embedded resource, ~8.2k domains
│   └── wwwroot/
│       ├── app.css                 # the entire design system (2384 lines)
│       ├── fonts/                  # fraunces-latin, public-sans-latin, jetbrains-mono-latin (woff2)
│       ├── js/                     # theme.js, site.js, prism.js, crop.js, colorpicker.js, md-input.js
│       ├── logo.png, favicon.ico, favicon-192.png, favicon-512.png, apple-touch-icon.png, social-card.png
└── tests/Portfolio.Tests/          # 35 xUnit files (34 fixtures + UnitTest1.cs scaffold)
```

## `src/Portfolio.Web/Components`

| Folder | Files | Convention |
|---|---|---|
| `Components/` (root) | `App`, `Routes`, `_Imports`, `LandingSections`, `SocialMeta`, `Icon`, `CommentSection`, `MarkdownInput`, `NotFoundContent`, `Pager`, `PagerControls`, `SortHeader` | Shared, non-routable components. Anything rendered by more than one page lives here |
| `Components/Pages/` | `Home`, `Projects`, `Blog`, `BlogPostPage`, `Contact`, `SignIn`, `Privacy`, `Terms`, `NotFound`, `Error`, `Profile`, `Messages`, `MyReports` | Public and account pages. Public pages have no `@rendermode` (static SSR); the three account pages are `[Authorize]` + `@rendermode InteractiveServer` |
| `Components/Admin/` | `Dashboard`, `Posts`, `PostEditor`, `ProjectsAdmin`, `ProjectEditor`, `CommentsAdmin`, `Messages`, `ReportsAdmin`, `Stats`, `SiteContentEditor`, `ThemeEditor`, `ImageCropField` | All `[Authorize(Roles = "Admin")]`; every page except `Dashboard` is `InteractiveServer`. `ImageCropField` is a shared admin widget, not a page |
| `Components/Layout/` | `MainLayout`, `ReconnectModal` (+ scoped css/js) | One layout for the whole site; the reconnect modal replaces the framework default |

Page shape: `@page` → `@inject`s → `<PageTitle>` + `<SocialMeta>` → sections built from `.container`, `.section`, `.eyebrow`, `.page-head` → `@code` block. Pages that read the database do it in `OnInitializedAsync` through a singleton service, never with a DbContext in the component.

## `src/Portfolio.Web/Services` (48 files)

Two families with a deliberate split:

| Family | Naming | Dependencies | Tested by |
|---|---|---|---|
| Pure rules | `*Rules.cs` (`AnalyticsRules`, `CommentRules`, `ContactSpamRules`, `PostRules`, `ProfileRules`, `ProjectRules`, `ProjectUrlRules`, `ReportRules`, `SeoRules`, `SiteContentRules`, `ThemeRules`) plus helpers (`BadgeLabel`, `BlogFilters`, `IconKind`, `JsModuleUrl`, `PagedResult`, `PagerWindow`, `SlugHelper`, `Sorting`, `VisitorKey`, `AnalyticsRollup`, `EmailTemplates`, `ImageGuards`) | None (static classes or records; no DbContext, no IO) | Almost the entire test suite |
| Services | `*Service.cs` (`AnalyticsService`, `AvatarService`, `BlogService`, `CommentService`, `ContactService`, `EmailService`, `ImageUploadService`, `MessageService`, `ModerationService`, `OwnerPhotoService`, `ProfileService`, `ProjectService`, `ReportService`, `SiteContentService`, `ThemeService`, `MarkdownService`) plus `AnalyticsMiddleware`, `AnalyticsRollupService` (hosted), `ContactRateLimiter`, `ContactFormTimestamp`, `DisposableEmailDomains`, `MailDomainChecker` (+ `IMxResolver`), `AdminEmails`, `OAuthProviders`, `SiteConfig`, `DemoSeeder` | `IDbContextFactory<AppDbContext>`, filesystem, SMTP, DNS, `TimeProvider` | Only the three filesystem services (`AvatarService`, `ImageUploadService`, `OwnerPhotoService`) and the clock-driven guards (`ContactRateLimiter`, `ContactFormTimestamp`, `MailDomainChecker`, `DisposableEmailDomains`) |

Every service is registered `AddSingleton` in `Program.cs:17-45`. `ThemeService` and `SiteContentService` hold in-process caches on the assumption of a single container.

## `src/Portfolio.Web/Data`

`AppDbContext : IdentityDbContext<ApplicationUser>` with 15 `DbSet`s: `BlogPosts`, `Projects`, `Comments`, `ContactMessages`, `Reports`, `UserMessages`, `SiteContents` (singleton row), `ThemeSettings` (singleton row, jsonb), `AnalyticsStates` (singleton row), `PageViews`, `AnalyticsEvents`, `DailySiteStats`, `DailyRouteStats`, `DailyReferrerStats`, `DailyEventStats`. Entity classes are plain POCOs; configuration (indexes, lengths, `jsonb`, `ValueGeneratedNever` on singleton ids) lives in `OnModelCreating`.

## `src/Portfolio.Web/Endpoints`

Static extension classes (`MapAuthEndpoints`, `MapSeoEndpoints`, `MapAnalyticsEndpoints`) with minimal-API lambdas. They take services by parameter injection and return `Results.*`. See `api-documentation.md`.

## `src/Portfolio.Web/wwwroot/js`

| File | Loaded how | Role |
|---|---|---|
| `theme.js` | `<script>` in `<head>`, blocking, before `app.css` | Sets `data-theme` on `<html>` from `localStorage['theme']` or `prefers-color-scheme` before first paint; exposes `__applyTheme` / `__toggleTheme` |
| `site.js` | `<script defer>` at end of body | Post-navigation glue: `enhancedload` hook re-applies theme, closes the mobile nav, localizes `<time data-local>`, runs Prism; `MutationObserver` for interactive islands; `__toggleNav`, `__scrollProjects` |
| `prism.js` | `<script defer>` | Vendored highlighter bundle |
| `crop.js` | ES module imported per circuit from `ImageCropField.razor` | 16:9 crop tool with canvas bake |
| `colorpicker.js` | ES module imported per circuit from `ThemeEditor.razor` | HSV picker dialog writing hex into the token inputs |
| `md-input.js` | ES module imported per circuit from `MarkdownInput.razor` | Overlay-mirror live markdown display tokenizer |

Per-circuit modules follow one lifecycle: import in `OnAfterRenderAsync` via `JsModuleUrl.Resolve(Assets[...])`, `init`/`open`/`refresh`, `IAsyncDisposable` dispose, and a silent fallback to plain controls when the import fails.

## `tests/Portfolio.Tests`

One file per unit under test, named `<Type>Tests.cs`. 250 `[Fact]`/`[Theory]` methods, 223 `[InlineData]` rows. Filesystem tests use temp directories; clock-driven tests use `FakeTimeProvider`. No component rendering, HTTP, or database tests exist (see `code-quality-assessment.md`). `UnitTest1.cs` is the untouched template scaffold.

## Conventions worth knowing before changing anything

- Blank hides: optional content (`About`, `Skills`, owner photo, sponsor link, résumé, phone) is rendered only when set; a blank admin override falls back to the env value and cannot force-blank a non-empty env value (`Services/SiteContentRules.cs:19-22`).
- Personal details never live in code: `SiteConfig` is built from environment variables at startup and validated (`SITE_OWNER_NAME`, `CONTACT_EMAIL` required).
- `CONTEXT.md` names are canonical in code and copy: Owner Photo (not avatar), Pinned Comment, Hard Signal, Fake Success, Soft Signal, Quarantined Message, Render Token, Disposable Domain, Visitor Key, Daily Visitor, Named Event, Rollup, Watermark.
- Length caps are single-sourced in the `*Rules` classes and reused by both validation and the UI (`maxlength`).
- Schema changes always ship as an EF migration, applied at startup.
- No inline `<script>` beyond the JSON-LD blocks; the four inline `onclick` attributes (`MainLayout.razor:16, 63`; `Projects.razor:36, 97`) are the only inline handlers.
- Every new animation is registered in the single `prefers-reduced-motion` block (`app.css:282-285`).
