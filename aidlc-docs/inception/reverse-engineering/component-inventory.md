# Component Inventory

Snapshot at `master` @ `e1825d9` (v1.22.0). Paths are relative to `src/Portfolio.Web/` unless stated. "SSR" means static server-side rendering (no `@rendermode`); "Interactive" means `@rendermode InteractiveServer`.

## Razor pages: public

| Route | File | Render | Auth | Purpose | Main dependencies |
|---|---|---|---|---|---|
| `/` | `Components/Pages/Home.razor` | SSR | none | Thin shell: `PageTitle`, `SocialMeta` with `Person` + `WebSite` JSON-LD, then `<LandingSections Content="_content" />`. Seeds content synchronously from env, then replaces it with the DB-merged copy | `SiteConfig`, `SiteContentService`, `OwnerPhotoService`, `NavigationManager`, `IConfiguration` |
| `/projects` | `Components/Pages/Projects.razor` | SSR | none | h1 "Things I've built"; scroll-snap carousel with prev/next buttons (`__scrollProjects`); per-card Homepage/Repo links through `/go/{id}/{kind}` with `data-enhance-nav="false"`; admin-only edit badge; "See everything on GitHub" CTA | `ProjectService`, `SiteConfig` |
| `/blog` | `Components/Pages/Blog.razor` | SSR | none | h1 "Notes & write-ups"; GET-form filters (`q`, `month`, `tag`), removable filter chips, link-based `Pager` | `BlogService`, `BlogFilters`, `MarkdownService` (reading time) |
| `/blog/{Slug}` | `Components/Pages/BlogPostPage.razor` | SSR (comments island Interactive) | none | Hero image with alt, tags, reading time, `.prose` body from the trusted pipeline, `BlogPosting` JSON-LD, `CommentSection`; admin "Edit post" link; 404 on unknown slug | `BlogService`, `MarkdownService` |
| `/contact` | `Components/Pages/Contact.razor` | SSR | none | h1 "Say hello"; `EditForm` (Name, Email, Subject, Message) with honeypot and render token; "Elsewhere" aside (email, résumé, phone, LinkedIn, GitHub); success copy "I read everything and reply when I can." | `ContactService`, `ContactSpamRules`, `ContactFormTimestamp`, `ContactRateLimiter`, `MailDomainChecker`, `DisposableEmailDomains`, `SiteConfig` |
| `/signin` | `Components/Pages/SignIn.razor` | SSR | none | h1 "Join the conversation"; one button per enabled provider; error copy for `external`, `noemail`, `create` | `OAuthProviders` |
| `/privacy` | `Components/Pages/Privacy.razor` | SSR | none | h1 "What this site knows about you"; seven sections describing the actual data handling | `SiteConfig` |
| `/terms` | `Components/Pages/Terms.razor` | SSR | none | h1 "The house rules" | `SiteConfig` |
| `/not-found` | `Components/Pages/NotFound.razor` | SSR | none | Renders `NotFoundContent`; stamps 404 on direct visits only | `IHttpContextAccessor`-style status feature |
| `/Error` | `Components/Pages/Error.razor` | SSR | none | Framework error page with request id | none |

## Razor pages: account (`[Authorize]`, Interactive)

| Route | File | Purpose | Main dependencies |
|---|---|---|---|
| `/profile` | `Components/Pages/Profile.razor` | h1 "How you appear": display name, avatar upload/remove, post-anonymously default | `ProfileService`, `AvatarService`, `ProfileRules` |
| `/messages` | `Components/Pages/Messages.razor` | h1 "Your inbox": moderation and system messages, markdown-rendered through the UGC pipeline | `MessageService`, `MarkdownService` |
| `/my-reports` | `Components/Pages/MyReports.razor` | h1 "Reports you've filed": status and admin responses | `ReportService`, `MarkdownService` |

## Razor pages: admin (`[Authorize(Roles = "Admin")]`)

| Route | File | Render | Purpose | Main dependencies |
|---|---|---|---|---|
| `/admin` | `Components/Admin/Dashboard.razor` | SSR | Eight card links with badges (unread and flagged messages, open reports, 7-day views) | `ContactService`, `ReportService`, `AnalyticsService` |
| `/admin/posts` | `Components/Admin/Posts.razor` | Interactive | Sortable, filterable, paged post list | `BlogService`, `Sorting`, `PagerControls`, `SortHeader` |
| `/admin/posts/new`, `/admin/posts/{Id:int}/edit` | `Components/Admin/PostEditor.razor` | Interactive | Title, slug, summary, tags, draft flag, `MarkdownInput` body, `ImageCropField` header image + alt | `BlogService`, `PostRules`, `SlugHelper`, `ImageUploadService` |
| `/admin/projects` | `Components/Admin/ProjectsAdmin.razor` | Interactive | Reorder, visibility toggle, list | `ProjectService` |
| `/admin/projects/new`, `/admin/projects/{Id:int}/edit` | `Components/Admin/ProjectEditor.razor` | Interactive | Title, summary, URLs, `ImageCropField` image + alt | `ProjectService`, `ProjectRules`, `ProjectUrlRules` |
| `/admin/comments` | `Components/Admin/CommentsAdmin.razor` | Interactive | Hide, delete, pin; sortable table | `CommentService`, `ModerationService` |
| `/admin/messages` | `Components/Admin/Messages.razor` | Interactive | Contact inbox; Flagged filter; "Not spam"; read state | `ContactService`, `MarkdownService` |
| `/admin/reports` | `Components/Admin/ReportsAdmin.razor` | Interactive | Report queue, bans, user-message composers (largest page, ~395 lines) | `ReportService`, `ModerationService`, `MessageService`, `MarkdownInput` |
| `/admin/stats` | `Components/Admin/Stats.razor` | Interactive | Period select, five stat tiles, sortable top pages, referrers, events | `AnalyticsService` |
| `/admin/site` | `Components/Admin/SiteContentEditor.razor` | Interactive | Hero heading, tagline, about, skills, photo alt overrides; owner photo upload/remove | `SiteContentService`, `SiteContentRules`, `OwnerPhotoService` |
| `/admin/theme` | `Components/Admin/ThemeEditor.razor` | Interactive | Brand / Dark / Light token groups with swatches, hex inputs and the `colorpicker.js` dialog; live `LandingSections` preview in either mode inside an `inert aria-hidden` frame; WCAG contrast warnings; save and reset | `ThemeService`, `ThemeRules`, `SiteContentService`, `JsModuleUrl` |

## Shared components (non-routable)

| File | Render | Used by | Purpose |
|---|---|---|---|
| `Components/App.razor` | SSR root | framework | Assembles `<head>`: meta, canonical + `og:url` (query stripped), `twitter:card`, RSS link, `ResourcePreloader`, `theme.js` (blocking), `app.css`, conditional admin override `<style>`, scoped-CSS bundle, `ImportMap`, icons and manifest, `HeadOutlet`; body: `Routes`, `ReconnectModal`, `blazor.web.js`, `prism.js` and `site.js` (deferred). Reads the theme snapshot from `ThemeService` |
| `Components/Routes.razor` | SSR | `App` | `Router` with `NotFoundPage`, `AuthorizeRouteView` + `MainLayout`; `NotAuthorized` renders `NotFoundContent`; `FocusOnNavigate Selector="h1"` |
| `Components/LandingSections.razor` | inherits host | `Home`, `ThemeEditor` (preview) | Hero (ribbons, h1, tagline, CTA row, optional owner photo) and About + Skills; shared verbatim so the admin preview cannot drift |
| `Components/SocialMeta.razor` | SSR head | every public page | `description`, `og:title/description/type/image(:alt)`, `article:*` times, JSON-LD as child content (only the last `HeadContent` wins, hence child content) |
| `Components/Icon.razor` | any | CTAs, footer, sign-in, contact aside, admin | Inline SVG by kind: `github`, `google`, `discord`, `linkedin` (Simple Icons, filled), `email`, `phone`, `file`, `pin` (stroke), `heart` (filled), fallback external-link glyph. All `aria-hidden="true"`, sized `1em` by `.icon` |
| `Components/CommentSection.razor` | Interactive island | `BlogPostPage` | Pinned section, newest-first list, "Show more" paging, anonymous posting, delete own, report, ban check, `MarkdownInput` composer with preview |
| `Components/MarkdownInput.razor` | Interactive | `CommentSection`, `PostEditor`, `ReportsAdmin` | Overlay-mirror live markdown composer; imports `md-input.js` per circuit; forces `--font-mono` for caret metric parity |
| `Components/NotFoundContent.razor` | SSR | `NotFound`, `Routes` | Eyebrow "404", h1 "There's nothing here", "Back to home" |
| `Components/Pager.razor` | SSR | `Blog` | Link-based pager preserving filters (no JS) |
| `Components/PagerControls.razor` | Interactive | admin tables | Callback pager |
| `Components/SortHeader.razor` | Interactive | admin tables | Sortable column header with the accent glow hover |
| `Components/Layout/MainLayout.razor` | SSR | all pages | Header (logo-only brand with `aria-label`, burger, nav Projects/Blog/Contact, admin link with attention badge, avatar chip + unread badge, logout form, theme toggle), `<main>`, footer (four-color strip, copyright, social nav, legal nav), `#blazor-error-ui`. Runs two to four DB queries per render for badges |
| `Components/Layout/ReconnectModal.razor` (+ `.razor.css`, `.razor.js`) | framework | `App` | Custom reconnect dialog; stock-styled and always light (untokenized) |
| `Components/Admin/ImageCropField.razor` | Interactive | `PostEditor`, `ProjectEditor` | 16:9 crop box, zoom/pan, rule-of-thirds and hero-band guides, canvas bake to a hidden `InputFile`; imports `crop.js` per circuit; falls back to a plain file input |

## Services (`Services/`, all registered as singletons in `Program.cs:17-45` unless stated)

| Service | Depends on | Purpose |
|---|---|---|
| `SiteConfig` (record, built by `FromConfiguration`) | `IConfiguration` | Owner name, title, tagline, meta description, about (`\n` expanded), skills, contact email and phone, social URLs, sponsor, résumé path, owner photo path and alt; throws when `SITE_OWNER_NAME` or `CONTACT_EMAIL` is missing |
| `AdminEmails` | `IConfiguration` | Comma-separated `ADMIN_EMAILS` membership check |
| `OAuthProviders` (+ `OAuthProvider`, `ReadCredentials`) | `IConfiguration` | Enabled provider list for the sign-in page and `/auth/login` |
| `MarkdownService` | Markdig | Trusted pipeline, UGC pipeline with AST guard, `ToPlainText`, reading time |
| `BlogService` | `IDbContextFactory<AppDbContext>` | Posts: list with filters and paging, get by slug, latest published, published slugs, admin CRUD |
| `CommentService` | DbContext factory | Comment CRUD with pinned ordering, ban checks |
| `ProjectService` | DbContext factory | Visible and admin project lists, ordering, CRUD |
| `ContactService` | DbContext factory, `EmailService` | Store messages (with quarantine flag), unread counts, admin inbox |
| `ContactRateLimiter` | `TimeProvider` | Per-IP fixed-window limiter (in memory) |
| `ContactFormTimestamp` | `IDataProtectionProvider`, `TimeProvider` | Signed render-time token; undecipherable tokens quarantine |
| `ContactSpamRules` (static) | none | Minimum submit time, link-heavy body and subject-URL heuristics |
| `DisposableEmailDomains` | embedded resource | Parent-suffix domain matching over ~8.2k domains |
| `MailDomainChecker` / `IMxResolver` / `DnsClientMxResolver` | DnsClient, `TimeProvider` | MX then A/AAAA presence check, 2.5 s cap, fail-open |
| `EmailService` | MailKit, `IConfiguration` | SMTP send; no-op when host is blank; failures logged and non-fatal |
| `EmailTemplates` (static) | none | Branded HTML notification with a hardcoded light palette and web-safe fonts |
| `ImageUploadService` | filesystem (`Uploads__Path`) | Validates extension and size, writes GUID-named files under `/uploads`; `.svg` and `.gif` pass through un-decoded |
| `AvatarService` | ImageSharp, filesystem | Buffered decode with size guard, auto-orient, center crop, square WebP |
| `OwnerPhotoService` | `SiteConfig`, ImageSharp, filesystem | Single owner photo at `OWNER_PHOTO_FILE`; `GetVersionedUrl()` (`/owner-photo?v=ticks`), replace/remove, `SniffContentType` |
| `ImageGuards` (static) | ImageSharp | Decoded-size limits |
| `ProfileService` / `ProfileRules` | DbContext factory | Commenter profile (custom name, avatar, anonymous default) |
| `MessageService` | DbContext factory | User inbox, unread counts |
| `ReportService` / `ReportRules` | DbContext factory | Reports with excerpt snapshots, open counts, resolution |
| `ModerationService` | DbContext factory | Hide/delete/pin comments, bans, user messages |
| `SiteContentService` / `SiteContentRules` | DbContext factory, `SiteConfig` | Singleton row overrides merged over env; in-process cache with version guard; falls back to env on DB failure |
| `ThemeService` / `ThemeRules` | DbContext factory | 26-token catalog, override validation, snapshot (`OverrideCss`, `MetaThemeColor`), preview style, WCAG contrast warnings; cache; falls back to defaults on DB failure without logging |
| `AnalyticsMiddleware` | `AnalyticsService` | Records qualifying page views after the response |
| `AnalyticsService` / `AnalyticsRules` / `VisitorKey` | DbContext factory, `TimeProvider` | Page views and named events (`project-click`, `resume-download`, `contact-submit`); daily-rotating HMAC visitor keys; DNT/GPC and bot exclusions; stats queries |
| `AnalyticsRollup` (static) / `AnalyticsRollupService` (hosted) | DbContext factory, `TimeProvider` | Aggregation, nightly schedule at 00:20 UTC, startup catch-up, 90-day raw retention |
| `DemoSeeder` (static) | DbContext | Two sample posts and two sample projects when `SEED_DEMO_DATA=true` and tables are empty |
| Helpers: `BadgeLabel`, `BlogFilters`, `IconKind`, `JsModuleUrl`, `PagedResult`, `PagerWindow`, `PostRules`, `ProjectRules`, `ProjectUrlRules`, `CommentRules`, `SeoRules`, `SlugHelper`, `Sorting` | none | Pure, unit-tested logic used by pages and services |

## Data entities (`Data/`)

| Entity | Key fields | Notes |
|---|---|---|
| `ApplicationUser : IdentityUser` | `DisplayName`, `CustomDisplayName`, `AvatarUrl`, `PostAnonymouslyByDefault`, `IsBanned`, `BannedAt`, `BanReason`; computed `PublicName` | Commenters, not the owner |
| `BlogPost` | `Slug` (unique), `Title`, `Summary`, `BodyMarkdown`, `HeaderImagePath`, `HeaderImageAlt`, `Tags` (`List<string>`), `IsPublished`, `CreatedAt`, `UpdatedAt`, `PublishedAt` | Ordering by `PublishedAt` |
| `Project` | `Title`, `Summary`, `HeaderImagePath`, `HeaderImageAlt`, `HomepageUrl`, `RepoUrl`, `SortOrder`, `IsVisible` | Card-only model; no body |
| `Comment` | `BlogPostId`, `UserId`, `Body`, `IsHidden`, `IsPinned`, `IsAnonymous`, timestamps | Pinned is independent of hidden |
| `ContactMessage` | `Name`, `Email`, `Subject`, `Body`, `ReceivedAt`, `IsRead`, `IsFlagged`, `FlagReason` | Quarantine via `IsFlagged` |
| `Report` | `ReporterId`, `TargetUserId`, `CommentId?`, `CommentExcerpt`, `TargetType`, `Reason`, `Details`, `Status`, `ResolvedAt` | Excerpt survives comment deletion |
| `UserMessage` | `RecipientId`, `SenderId?` (null = system), `Body`, `QuotedComment`, `ReportId?`, `IsRead` | Rendered through the UGC pipeline |
| `SiteContent` | singleton `Id=1`; `HeroHeading`, `Tagline`, `About`, `Skills`, `OwnerPhotoAlt`, `UpdatedAt` | Overrides env values when non-null |
| `ThemeSettings` | singleton `Id=1`; `Overrides` (jsonb dictionary), `UpdatedAt` | Token key to `#rrggbb` |
| `AnalyticsState` | singleton `Id=1`; `Secret` | Per-install HMAC key |
| `PageView`, `AnalyticsEvent` | `Path` / `Name` + `Target`, `ReferrerHost`, `VisitorKey`, `OccurredAt` | Raw rows, 90-day retention |
| `DailySiteStat`, `DailyRouteStat`, `DailyReferrerStat`, `DailyEventStat` | `Day` (+ path / referrer host / event name and target), counts | Permanent aggregates; `DailySiteStat.Day` doubles as the rollup watermark |

## JavaScript (`wwwroot/js/`)

| File | Lines | Globals or exports | Consumers |
|---|---|---|---|
| `theme.js` | 27 | `window.__applyTheme`, `window.__toggleTheme`; `localStorage['theme']` | `App.razor` head, `MainLayout` toggle, `site.js` |
| `site.js` | 106 | `window.__toggleNav`, `window.__scrollProjects`; `enhancedload` hook; `MutationObserver` | `MainLayout` burger, `Projects` arrows, every page (time localization, Prism) |
| `prism.js` | vendored | `window.Prism` | `site.js`, `md-input.js` |
| `crop.js` | 533 | `init(prefix, maxBytes)`, `open(prefix, url)` | `ImageCropField.razor` |
| `colorpicker.js` | 249 | `init(dialogId)`, `open(hex, targetInputId)` | `ThemeEditor.razor` |
| `md-input.js` | 187 | `init(id)`, `refresh(id)`, `dispose(id)` | `MarkdownInput.razor` |
| `Components/Layout/ReconnectModal.razor.js` | 63 | reconnect state machine | `ReconnectModal.razor` |

## Icon kinds (`Services/IconKind.cs`, `Components/Icon.razor`)

`github`, `google`, `discord`, `linkedin`, `email`, `phone`, `file`, `pin`, `heart`; any other value (including OAuth scheme names after `Normalize`) falls back to `external`. The header's hamburger, sun and moon glyphs are hand-inlined in `MainLayout.razor` rather than routed through `Icon`.
