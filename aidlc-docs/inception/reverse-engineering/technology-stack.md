# Technology Stack

Snapshot at `master` @ `e1825d9` (v1.22.0). Versions are the ones pinned in the repository; see `dependencies.md` for the full package table.

## Runtime and framework

| Layer | Choice | Where it is pinned or configured | Notes |
|---|---|---|---|
| SDK | .NET SDK 10.0.302, `rollForward: latestFeature` | `global.json` | Installed via winget during Unit 1; CI uses `actions/setup-dotnet@v4` with `global-json-file` |
| Target framework | `net10.0` | `src/Portfolio.Web/Portfolio.Web.csproj`, `tests/Portfolio.Tests/Portfolio.Tests.csproj` | `Nullable` and `ImplicitUsings` enabled in both projects |
| Web framework | ASP.NET Core Blazor Web App | `Program.cs:14-15` (`AddRazorComponents().AddInteractiveServerComponents()`), `Program.cs:241-242` (`MapRazorComponents<App>().AddInteractiveServerRenderMode()`) | Static server-side rendering by default; `InteractiveServer` opted into per component (14 components). No WebAssembly project, no streaming rendering |
| Enhanced navigation | Blazor `blazor.web.js` | `Components/App.razor` | Links that must bypass it carry `data-enhance-nav="false"` (`/go/*`, `/resume`) |
| Data access | Entity Framework Core 10 with the Npgsql provider | `Program.cs:52-60` | `AddDbContextFactory` for components plus a singleton-lifetime `AddDbContext` for Identity stores; `EnableDynamicJson()` for the jsonb dictionary on `ThemeSettings` |
| Database | PostgreSQL 17 (`postgres:17-alpine`) | `docker-compose.yml` | 18 migrations under `src/Portfolio.Web/Migrations/`, applied by `db.Database.Migrate()` at startup (`Program.cs:152`) |
| Identity | ASP.NET Core Identity Core with roles, external login only | `Program.cs:79-101` | No password accounts; `LoginPath=/signin`; Admin role from `ADMIN_EMAILS` |
| OAuth providers | GitHub, Google, Discord | `Program.cs:104-141` | Each registered only when both client id and secret exist in configuration |
| Markdown | Markdig | `Services/MarkdownService.cs` | Trusted pipeline (`UseAdvancedExtensions`) for admin posts; restricted UGC pipeline (`DisableHtml`, AST guard) for comments and messages |
| Images | SixLabors.ImageSharp | `Services/ImageUploadService.cs`, `AvatarService.cs`, `OwnerPhotoService.cs`, `ImageGuards.cs` | Decode-size guards; EXIF stripped by re-encoding to WebP for avatars and the owner photo |
| Email | MailKit (SMTP) | `Services/EmailService.cs`, `Services/EmailTemplates.cs` | No-op when `SMTP__HOST` is blank; branded HTML templates with a hardcoded light palette |
| DNS | DnsClient | `Services/MailDomainChecker.cs` | MX then A/AAAA lookup, 2.5 s cap, fail-open |
| Health | `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | `Program.cs:144-145, 240` | `/healthz` with a DbContext probe; used by the compose healthcheck |
| Data protection | ASP.NET Core Data Protection, file-system key ring | `Program.cs:63-68` | Keys persisted to the `dpkeys` volume (`DataProtection__KeysPath=/app/keys`) so cookies survive restarts; also signs the contact-form render token |
| Background work | `BackgroundService` | `Services/AnalyticsRollupService.cs` | The app's only hosted service: nightly rollup at 00:20 UTC with startup catch-up |

## Front end

| Concern | Choice | Where |
|---|---|---|
| CSS | One hand-written stylesheet, no preprocessor, no framework | `wwwroot/app.css` (2384 lines); one scoped file `Components/Layout/ReconnectModal.razor.css` |
| Design tokens | CSS custom properties on `:root`, light overrides on `:root[data-theme='light']` | `app.css:31-75` |
| Fonts | Fraunces (display), Public Sans (body), JetBrains Mono (labels, code); self-hosted latin-subset variable woff2 | `wwwroot/fonts/*`, `app.css:4-26` |
| Icons | Inline SVG only (Simple Icons path data for brands, hand-drawn strokes for utility glyphs) | `Components/Icon.razor`, `Services/IconKind.cs` |
| Syntax highlighting | Prism (vendored, minified bundle) | `wwwroot/js/prism.js`; theme mapped to tokens in `app.css:923-937` |
| JavaScript | Plain ES2015+, no bundler, no package manager | `wwwroot/js/theme.js` (blocking, pre-paint), `site.js` (deferred glue), and three per-circuit ES modules `crop.js`, `colorpicker.js`, `md-input.js` imported through `Services/JsModuleUrl.cs` |
| Third-party network calls from the browser | None | No CDN, no analytics script, no external fonts |

## Build, test, delivery

| Concern | Choice | Where |
|---|---|---|
| Solution | `Portfolio.slnx` (XML solution format) | repository root |
| Tests | xUnit 2.9.3, `Microsoft.NET.Test.Sdk`, coverlet collector, `Microsoft.Extensions.TimeProvider.Testing` | `tests/Portfolio.Tests/Portfolio.Tests.csproj` |
| Local tools | `dotnet-ef` 10.0.10 | `dotnet-tools.json` |
| CI | GitHub Actions: restore, `dotnet build -warnaserror`, `dotnet test`; PR title check (`amannn/action-semantic-pull-request@v5`) | `.github/workflows/ci.yml` |
| Release | release-please (`release-type: simple`) creates version PRs and tags; on release creation the same workflow tests the tag and pushes `ghcr.io/jjwren/portfolio:{version}` and `:latest` | `.github/workflows/release-please.yml` |
| Container | Multi-stage Dockerfile; app listens on HTTP 8080; TLS at the reverse proxy | `Dockerfile`, `docker-compose.yml`, `Program.cs:204-208` |
| Volumes | `pgdata`, `uploads` (`/app/uploads`), `dpkeys` (`/app/keys`) | `docker-compose.yml` |
| Configuration | Environment variables (double-underscore sections), `.env` via compose `env_file` | `.env.example`, `Services/SiteConfig.cs` |
| Review gate | Copilot review on every PR until zero actionable comments, then squash-merge with a conventional-commit title | `CONTRIBUTING.md` |

## Not used (deliberately or by omission)

- No Bootstrap, Tailwind, or component library (removed in Unit 2).
- No client-side analytics vendor (rejected in `docs/adr/0001-first-party-cookieless-analytics.md`).
- No `HtmlSanitizer` dependency (dropped for a transitive AngleSharp CVE; replaced by an AST-level guard, see `audit.md` 2026-07-25).
- No output caching, response compression, framework rate limiting, or security-header middleware (see `code-quality-assessment.md`).
- No bUnit, Playwright, or `WebApplicationFactory` tests.
