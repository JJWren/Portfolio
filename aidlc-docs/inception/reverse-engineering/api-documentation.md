# API Documentation

Snapshot at `master` @ `e1825d9` (v1.22.0). The application has no JSON API for clients; its HTTP surface is server-rendered pages, a handful of minimal-API endpoints, the Blazor circuit hub, and static assets. Everything is same-origin; there is no CORS configuration and no API authentication scheme other than the Identity cookie.

## Cross-cutting behaviour

| Concern | Behaviour | Source |
|---|---|---|
| HEAD requests | Served as GET with the body discarded (RFC 9110); the method is restored afterwards and the request is marked so analytics does not count it | `Program.cs:168-193` |
| Error handling | Non-development: `UseExceptionHandler("/Error")` and HSTS. All environments: `UseStatusCodePagesWithReExecute("/not-found")` | `Program.cs:196-202` |
| HTTPS | Redirect only in development; production terminates TLS at the reverse proxy and honours `X-Forwarded-For` / `X-Forwarded-Proto` | `Program.cs:71-76, 166, 205-208` |
| Auth | Identity application cookie (`LoginPath=/signin`, `ReturnUrlParameter=returnUrl`); external cookie for the OAuth handshake | `Program.cs:87-101` |
| Antiforgery | `UseAntiforgery()`; forms include `<AntiforgeryToken />` | `Program.cs:217` |
| Analytics | `AnalyticsMiddleware` after auth: records 200 HTML GETs by non-admins as page views; skips bot user agents, DNT/GPC, excluded prefixes, HEAD rewrites and status re-executes | `Program.cs:219-220`, `Services/AnalyticsMiddleware.cs`, `Services/AnalyticsRules.cs` |
| Canonical origin | `PUBLIC_BASE_URL` when set, else the request scheme and host | `Services/SeoRules.CanonicalOrigin`, `Endpoints/SeoEndpoints.cs:128-130` |
| Redirect safety | `returnUrl` must start with `/` and not `//`, else `/` | `Endpoints/AuthEndpoints.cs:115-119` |

## Minimal-API endpoints

### Authentication (`Endpoints/AuthEndpoints.cs`)

| Method | Route | Auth | Behaviour | Responses |
|---|---|---|---|---|
| GET | `/auth/login/{provider}?returnUrl=` | anonymous | Looks up the provider among the enabled OAuth schemes (case-insensitive); issues a challenge whose callback is `/auth/external-callback?returnUrl=<sanitized>` | 302 to the provider; 404 for unknown or disabled providers |
| GET | `/auth/external-callback?returnUrl=` | external cookie | Reads the external login; finds the user by login, then by email (so the same email on another provider joins the same account); creates the user when new (`EmailConfirmed=true`); links the login; refreshes `DisplayName` from the name claim; adds or removes the `Admin` role according to `ADMIN_EMAILS`; signs in persistently | 302 `LocalRedirect(returnUrl)`; 302 `/signin?error=external` (no external info), `?error=noemail` (provider returned no email), `?error=create` (user creation failed) |
| POST | `/auth/logout` (form field `returnUrl`) | antiforgery | Signs out | 302 `LocalRedirect(returnUrl)` |

Provider callback paths registered with the OAuth apps (handled by the authentication handlers, not by app code): `/signin-github`, `/signin-google`, `/signin-discord`.

### SEO and site files (`Endpoints/SeoEndpoints.cs`)

| Method | Route | Behaviour | Content type | Caching |
|---|---|---|---|---|
| GET | `/feed.xml` | RSS 2.0 with an Atom self link; latest 20 published posts (`title`, `link`, permalink `guid`, `pubDate` from `PublishedAt` or `CreatedAt`, `description` = summary) | `application/rss+xml; charset=utf-8` | none |
| GET | `/owner-photo` and `/owner-photo?v={ticks}` | Serves `OWNER_PHOTO_FILE`; content type is sniffed from the first 12 bytes, never trusted from the extension | image type from sniffing | `public, max-age=31536000, immutable` when `?v=` is present; `public, no-cache` otherwise (Last-Modified/ETag emitted) |
| GET | `/sitemap.xml` | `/`, `/projects`, `/blog`, `/contact`, `/terms`, `/privacy` plus every published post with `lastmod` | `application/xml; charset=utf-8` | none |
| GET | `/robots.txt` | `Disallow: /admin`, `/auth`, `/signin`; `Allow: /`; `Sitemap:` pointer on the canonical origin | `text/plain` | none |
| GET | `/site.webmanifest` | `name`/`short_name` from `SiteConfig`, `background_color` and `theme_color` from the effective dark theme (admin overrides included), icons 192 and 512 | `application/manifest+json; charset=utf-8` | none |

`/owner-photo` returns 404 when `OWNER_PHOTO_FILE` is unset, the file is missing, or the bytes cannot be identified as an image.

### Engagement and downloads (`Endpoints/AnalyticsEndpoints.cs`)

| Method | Route | Behaviour | Responses |
|---|---|---|---|
| GET | `/go/{id:int}/{kind}` (`kind` = `home` or `repo`) | Loads the visible project's URL from the database, validates it is http(s), records a `project-click` event with target `{Title}|{kind}`, redirects. Not an open redirect: the target comes solely from admin-edited columns | 302 to the project URL; 404 for other kinds, hidden or missing projects, or non-http URLs |
| GET | `/resume` | Config-gated by `RESUME_FILE`; records a `resume-download` event; returns the PDF as an attachment named after the file | 200 `application/pdf`; 404 when unset or missing |

Named events recorded by the analytics service (`Services/AnalyticsRules.cs:12-14`): `project-click`, `resume-download`, `contact-submit`.

### Operations

| Method | Route | Behaviour |
|---|---|---|
| GET | `/healthz` | ASP.NET health checks with a DbContext probe; used by the compose healthcheck (`curl -fsS`) |

### Static assets

| Route | Source | Caching |
|---|---|---|
| `/uploads/*` | Physical files under `Uploads__Path` (`/app/uploads` volume); GUID filenames are single-use | `public, max-age=31536000, immutable` (`Program.cs:224-235`) |
| `/app.css`, `/js/*`, `/fonts/*`, images | `wwwroot` via `MapStaticAssets` | fingerprinted, compressed, immutable per ASP.NET static web assets |
| `/_framework/*`, `/_blazor` | Blazor runtime script and the SignalR circuit hub for `InteractiveServer` components | framework defaults |

## Razor page routes

Render mode is static SSR unless stated. "Auth" is the attribute on the component; unauthorized visitors to role-gated pages receive the 404 body (`Components/Routes.razor`).

### Public

| Route | Component | Purpose |
|---|---|---|
| `/` | `Pages/Home.razor` | Landing: hero + about/skills via `LandingSections`; `Person` and `WebSite` JSON-LD |
| `/projects` | `Pages/Projects.razor` | Scroll-snap carousel of visible projects; links through `/go/{id}/{kind}` |
| `/blog` | `Pages/Blog.razor` | Paginated post list with `q`, `month`, `tag` GET filters |
| `/blog/{Slug}` | `Pages/BlogPostPage.razor` | Post page with `BlogPosting` JSON-LD; `CommentSection` island is `InteractiveServer` |
| `/contact` | `Pages/Contact.razor` | Contact form (POST via `EditForm`) with honeypot, render token, rate limit, MX and disposable-domain checks |
| `/signin` | `Pages/SignIn.razor` | Provider picker; renders `?error=` copy |
| `/privacy`, `/terms` | `Pages/Privacy.razor`, `Pages/Terms.razor` | Legal pages |
| `/not-found` | `Pages/NotFound.razor` | 404 body; stamps a real 404 status on direct visits |
| `/Error` | `Pages/Error.razor` | Exception page with request id |

### Account (`[Authorize]`, `InteractiveServer`)

| Route | Component | Purpose |
|---|---|---|
| `/profile` | `Pages/Profile.razor` | Display name, avatar upload/remove, post-anonymously default |
| `/messages` | `Pages/Messages.razor` | Moderation and system messages inbox |
| `/my-reports` | `Pages/MyReports.razor` | Reports filed by the user and their outcomes |

### Admin (`[Authorize(Roles = "Admin")]`)

| Route | Component | Render mode | Purpose |
|---|---|---|---|
| `/admin` | `Admin/Dashboard.razor` | static SSR | Card links with attention badges |
| `/admin/posts` | `Admin/Posts.razor` | InteractiveServer | Post list: sort, filter, page |
| `/admin/posts/new`, `/admin/posts/{Id:int}/edit` | `Admin/PostEditor.razor` | InteractiveServer | Post editor with live markdown composer and 16:9 crop field |
| `/admin/projects` | `Admin/ProjectsAdmin.razor` | InteractiveServer | Project list: reorder, visibility |
| `/admin/projects/new`, `/admin/projects/{Id:int}/edit` | `Admin/ProjectEditor.razor` | InteractiveServer | Project editor |
| `/admin/comments` | `Admin/CommentsAdmin.razor` | InteractiveServer | Hide, delete, pin |
| `/admin/messages` | `Admin/Messages.razor` | InteractiveServer | Contact inbox with Flagged filter and "Not spam" |
| `/admin/reports` | `Admin/ReportsAdmin.razor` | InteractiveServer | Report queue, bans, messages to users |
| `/admin/stats` | `Admin/Stats.razor` | InteractiveServer | Analytics dashboard |
| `/admin/site` | `Admin/SiteContentEditor.razor` | InteractiveServer | Landing copy overrides, owner photo upload |
| `/admin/theme` | `Admin/ThemeEditor.razor` | InteractiveServer | 26-token palette overrides with live landing preview and contrast warnings |

## Forms (POST targets)

| Form | Handler | Protections |
|---|---|---|
| Contact (`/contact`) | `EditForm` with `[SupplyParameterFromForm]` on the same page | Antiforgery, honeypot field, DataProtection-signed render timestamp (4 s minimum), per-IP fixed-window rate limit, MX check, disposable-domain list, link heuristics. Hard signals produce a fake success with nothing stored; soft signals store a quarantined (`IsFlagged`) message without SMTP |
| Logout (header) | `POST /auth/logout` | Antiforgery token, `returnUrl` sanitized |
| Comment, report, profile, admin editors | Blazor `InteractiveServer` event callbacks over the circuit | Identity cookie on the circuit; ban check in `CommentService` |
