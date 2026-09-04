# Dependencies

Snapshot at `master` @ `e1825d9` (v1.22.0). Every version below is read from the repository, not inferred.

## NuGet packages: `src/Portfolio.Web/Portfolio.Web.csproj`

| Package | Version | Used by | Purpose |
|---|---|---|---|
| `AspNet.Security.OAuth.GitHub` | 10.0.0 | `Program.cs:106-116` | GitHub OAuth handler (`user:email` scope) |
| `AspNet.Security.OAuth.Discord` | 10.0.0 | `Program.cs:129-139` | Discord OAuth handler (`email` scope) |
| `Microsoft.AspNetCore.Authentication.Google` | 10.0.10 | `Program.cs:118-127` | Google OAuth handler |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.10 | `Program.cs:79-85`, `Data/AppDbContext.cs` | Identity Core with roles on EF stores; external logins only |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | `Program.cs:52-60` | EF Core provider; `EnableDynamicJson()` for the `ThemeSettings.Overrides` jsonb dictionary |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.10 (`PrivateAssets=all`) | design time only | Migration scaffolding with `dotnet-ef` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.10 | `Program.cs:144-145` | `/healthz` DbContext probe |
| `Markdig` | 1.3.2 | `Services/MarkdownService.cs` | Trusted pipeline for posts (`UseAdvancedExtensions`), restricted UGC pipeline for comments and messages |
| `SixLabors.ImageSharp` | 3.1.12 | `Services/ImageUploadService.cs`, `AvatarService.cs`, `OwnerPhotoService.cs`, `ImageGuards.cs` | Identify, decode-size guard, auto-orient, crop, WebP re-encode |
| `MailKit` | 4.17.0 | `Services/EmailService.cs` | SMTP delivery of contact-form notifications |
| `DnsClient` | 1.8.0 | `Services/MailDomainChecker.cs` (`DnsClientMxResolver`) | MX then A/AAAA lookup for contact-form sender domains |

Transitive dependencies of note: the ASP.NET Core shared framework (Blazor, Data Protection, Antiforgery, Health Checks, Static Assets) and EF Core 10 relational core via the Npgsql provider. No `HtmlSanitizer`/AngleSharp: that dependency was dropped mid-implementation when NuGet audit under `-warnaserror` flagged the pinned AngleSharp for an mXSS CVE (`aidlc-docs/audit.md`, 2026-07-25); the UGC guard is implemented on Markdig's AST instead.

## NuGet packages: `tests/Portfolio.Tests/Portfolio.Tests.csproj`

| Package | Version | Purpose |
|---|---|---|
| `xunit` | 2.9.3 | Test framework (`[Fact]`, `[Theory]`, `[InlineData]`) |
| `xunit.runner.visualstudio` | 3.1.4 | Test adapter |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Test host |
| `coverlet.collector` | 6.0.4 | Coverage data collector |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.8.0 | `FakeTimeProvider` for `ContactRateLimiter`, `ContactFormTimestamp`, `MailDomainChecker`, `AnalyticsRollup` tests |

The test project references `Portfolio.Web` directly and relies on the ASP.NET Core shared framework flowing through that reference (for example `ConfigurationBuilder` in `SiteConfigTests.cs`). No bUnit, Playwright, or `Microsoft.AspNetCore.Mvc.Testing`.

## .NET tooling

| Tool | Version | Where |
|---|---|---|
| .NET SDK | 10.0.302 (`rollForward: latestFeature`) | `global.json` |
| `dotnet-ef` | 10.0.10 (local tool, `rollForward: false`) | `dotnet-tools.json` |

## Container images

| Image | Where | Purpose |
|---|---|---|
| `mcr.microsoft.com/dotnet/sdk:10.0` | `Dockerfile` (build stage) | restore + `dotnet publish -c Release` |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | `Dockerfile` (final stage) | runtime; adds `libgssapi-krb5-2` (Npgsql probes it) and `curl` (compose healthcheck); `ASPNETCORE_URLS=http://+:8080`, `Uploads__Path=/app/uploads` |
| `postgres:17-alpine` | `docker-compose.yml` | database with `pg_isready` healthcheck |
| `ghcr.io/jjwren/portfolio:{version}` and `:latest` | `.github/workflows/release-please.yml` | published application image |

## GitHub Actions

| Action | Version | Workflow |
|---|---|---|
| `actions/checkout` | v4 | `ci.yml`, `release-please.yml` |
| `actions/setup-dotnet` | v4 | `ci.yml`, `release-please.yml` (`global-json-file: global.json`) |
| `amannn/action-semantic-pull-request` | v5 | `ci.yml` (PR title types: feat, fix, chore, docs, refactor, test, ci, perf, build) |
| `googleapis/release-please-action` | v4 | `release-please.yml` (`release-type: simple`) |
| `docker/login-action` | v3 | `release-please.yml` (GHCR login with `GITHUB_TOKEN`) |

## Vendored and bundled assets (no package manager)

| Asset | Location | Notes |
|---|---|---|
| Prism syntax highlighter | `wwwroot/js/prism.js` | Minified bundle; languages available to the live composer per the audit log: js, ts, cs, py, sh, yml, html, css, sql, json. Theme colors mapped to tokens in `app.css:923-937` |
| Fraunces, Public Sans, JetBrains Mono | `wwwroot/fonts/*.woff2` | Latin-subset variable fonts, about 125 KB total (`unit2-theming-plan.md`) |
| Simple Icons path data | `Components/Icon.razor` | CC0 brand marks (GitHub, Google, Discord, LinkedIn) rendered in `currentColor` |
| Disposable email domain blocklist | `Resources/disposable-email-domains.txt` (embedded resource) | About 8.2k domains, parent-suffix matching in `Services/DisposableEmailDomains.cs` |
| Brand images | `wwwroot/logo.png`, `favicon.ico`, `favicon-192.png`, `favicon-512.png`, `apple-touch-icon.png`, `social-card.png` | Generated from the logo; the social card is the neutral ribbon fallback for `og:image` |

There is no `package.json`, no JS lockfile, and no build step for front-end assets; ASP.NET `MapStaticAssets` fingerprints and compresses `wwwroot` at publish time.

## External services at runtime

| Service | Required | Configuration | Failure behaviour |
|---|---|---|---|
| PostgreSQL | yes | `ConnectionStrings__Default` (compose wires it) | startup migration fails; at runtime `ThemeService` and `SiteContentService` fall back to built-in defaults |
| OAuth providers (GitHub, Google, Discord) | at least one for sign-in | `OAUTH__{PROVIDER}__CLIENTID` / `CLIENTSECRET` | provider not registered and not shown |
| SMTP host | no | `SMTP__HOST`, `SMTP__PORT`, `SMTP__USER`, `SMTP__PASSWORD`, `SMTP__FROM` | email disabled; messages still stored |
| DNS resolver (MX checks) | no | system resolver via DnsClient | fail-open after 2.5 s |
| Reverse proxy with TLS | production | `PUBLIC_BASE_URL`, forwarded headers | without `PUBLIC_BASE_URL` canonical, feed, sitemap and OG URLs use the request host |
| GHCR | delivery | `release-please.yml` | n/a |

## Update posture

- No Dependabot or Renovate configuration exists (`.github/dependabot.yml` absent).
- Vulnerability detection happens only incidentally: NuGet audit warnings become errors under `dotnet build -warnaserror` in CI (`ci.yml`), which is how the AngleSharp CVE surfaced.
- All package versions are hand-pinned; the Microsoft 10.0.x packages are on 10.0.10 while the Npgsql provider is on 10.0.3, both current for the .NET 10 line at the time of writing.

## Internal dependency direction

```text
Components (Pages, Admin, Layout, shared)
    │  inject
    ▼
Services (*Service singletons, SiteConfig, OAuthProviders, AdminEmails)
    │  use                           ▲ use
    ▼                                │
*Rules / helpers (pure)         Endpoints (minimal APIs)
    │
    ▼
Data (AppDbContext, entities)  ◄──  IDbContextFactory<AppDbContext>
```

Rules classes depend on nothing in the app; Services depend on Rules, Data, and infrastructure; Components and Endpoints depend on Services and Rules; nothing depends back on Components.
