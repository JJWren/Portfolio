# Portfolio

A self-hostable developer portfolio: home/about, projects carousel, markdown blog with
OAuth-backed comments, a contact form with an admin inbox, and a hidden admin area — all
personalized through environment variables so the same image works for anyone.

Built with ASP.NET Core Blazor (.NET 10) and PostgreSQL, shipped as a Docker Compose stack.

## Features

- **Dark-first theme** with a light-mode toggle, built on a five-color palette and
  locally bundled fonts (no CDN calls anywhere)
- **Blog**: markdown posts with drafts, tags, reading time, and locally bundled
  syntax highlighting; RSS feed at `/feed.xml`
- **Comments**: visitors sign in with GitHub, Google, or Discord (any subset you
  configure); authors delete their own, admins moderate everything
- **Projects**: admin-curated cards on a scroll-snap carousel with image upload,
  reordering, and visibility control
- **Contact form**: messages always stored in the database, email notification via
  SMTP on top (best-effort), honeypot + per-IP rate limiting
- **Hidden admin area** at `/admin` — invisible and 404 for everyone not in `ADMIN_EMAILS`
- SEO: per-page OpenGraph meta, `sitemap.xml`, `robots.txt`; `/healthz` health endpoint

## Quick start

```bash
git clone https://github.com/JJWren/Portfolio.git
cd Portfolio
cp .env.example .env
# Edit .env — see Configuration below
docker compose up -d
```

The site comes up on `http://localhost:8080` (override with `WEB_PORT`).
The database schema is created and migrated automatically on startup.
Set `SEED_DEMO_DATA=true` for sample content on first run.

## Configuration

Everything personal lives in `.env` — see [`.env.example`](.env.example) for the full
annotated list.

| Variable | Required | Purpose |
|---|---|---|
| `SITE_OWNER_NAME` | ✅ | Your name (hero, nav, footer) |
| `CONTACT_EMAIL` | ✅ | Contact-form notifications + mailto links |
| `POSTGRES_PASSWORD` | ✅ | Database password (compose wires the connection string) |
| `SITE_TITLE`, `SITE_TAGLINE`, `SITE_ABOUT`, `SITE_SKILLS` | | Copy for the home page (`\n` splits paragraphs; skills comma-separated) |
| `CONTACT_PHONE`, `LINKEDIN_URL`, `GITHUB_URL` | | Contact & social links; `GITHUB_URL` drives the projects-page CTA |
| `ADMIN_EMAILS` | | Comma-separated; matching OAuth emails get the hidden admin area |
| `OAUTH__<P>__CLIENTID` / `CLIENTSECRET` | | Enable sign-in per provider (`GITHUB`, `GOOGLE`, `DISCORD`) |
| `SMTP__HOST/PORT/USER/PASSWORD/FROM` | | Email notifications; blank host = DB-only mode |
| `PUBLIC_BASE_URL` | | Canonical origin for the feed/sitemap (e.g. `https://you.example`) |
| `SEED_DEMO_DATA` | | `true` seeds sample posts/projects into empty tables |

### OAuth callback URLs

Register an app with each provider you enable, using these callback/redirect URLs
(swap in your domain; `http://localhost:8080/...` works for local testing):

| Provider | Callback URL |
|---|---|
| GitHub | `https://your-domain/signin-github` |
| Google | `https://your-domain/signin-google` |
| Discord | `https://your-domain/signin-discord` |

The first time you sign in with an email listed in `ADMIN_EMAILS`, the **Admin**
link appears in the nav. Role membership re-syncs on every sign-in.

## Running behind a reverse proxy

The container serves plain HTTP on port 8080 and expects TLS to terminate at your
proxy (Caddy, Traefik, nginx…). Forwarded headers (`X-Forwarded-For` /
`X-Forwarded-Proto`) are honored so OAuth redirects build correct `https://` URLs.
Set `PUBLIC_BASE_URL` to your public origin.

## Operations

- **Health**: `GET /healthz` (also wired as the compose healthcheck) verifies the app + DB.
- **Data**: named volumes — `pgdata` (database), `uploads` (images), `dpkeys`
  (cookie encryption keys). Back up `pgdata` and `uploads`.
- **Images**: published to GHCR by CI on pushes to `master` (`latest`) and `v*` tags.

## Development

```bash
docker compose up -d db            # local postgres
dotnet run --project src/Portfolio.Web
dotnet test                        # unit tests
```

Requires the .NET 10 SDK (pinned in `global.json`). EF migrations:
`dotnet ef migrations add <Name> --project src/Portfolio.Web`.

## License & sponsoring

MIT — see [`LICENSE`](LICENSE). The site ships built-in [terms](/terms) and
[privacy](/privacy) pages that describe exactly what the software stores,
auto-personalized from your `.env`.

If this project is useful to you, you can
[buy me a coffee](https://buymeacoffee.com/jmykitta) ☕ — and self-hosters can
point the footer's sponsor link at their own page via `SPONSOR_URL` /
`SPONSOR_TEXT` (or leave it blank to hide it).
