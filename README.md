# Portfolio

A self-hostable developer portfolio: home/about, projects carousel, markdown blog with
OAuth-backed comments, a contact form with an admin inbox, and a hidden admin area — all
personalized through environment variables so the same image works for anyone.

Built with ASP.NET Core Blazor (.NET 10) and PostgreSQL, shipped as a Docker Compose stack.

## Features

- **Dark-first theme** with a light-mode toggle, built on a five-color palette and
  locally bundled fonts (no CDN calls anywhere)
- **Blog**: markdown posts with drafts, tags, reading time, header images, and
  locally bundled syntax highlighting; the list adds search, month, and tag filters
  with pagination; RSS feed at `/feed.xml`
- **Comments**: visitors sign in with GitHub, Google, or Discord (any subset you
  configure); profiles carry a display name and avatar, or comments can be posted
  anonymously; comments run newest-first with admin-pinned comments on top;
  authors delete their own, admins moderate everything
- **Moderation**: readers report comments; admins work a queue with notification
  badges, hide/delete controls, site-wide bans, and per-user messages — users track
  replies and report outcomes on their own messages and reports pages
- **Projects**: admin-curated cards on a scroll-snap carousel with image upload,
  reordering, and visibility control
- **Image cropping**: blog headers and project images go through a built-in 16:9
  crop-box editor — zoom, drag, rule-of-thirds guides, and live previews of exactly
  what the card and hero will show
- **Contact form**: messages always stored in the database, branded HTML email
  notification via SMTP on top (best-effort); spam defenses include a honeypot,
  per-IP rate limiting, a timing check, and sender-domain MX validation;
  suspicious-but-plausible messages (disposable domains, link-heavy bodies) are
  quarantined for admin review instead of emailed
- **Privacy-preserving analytics**: server-side, cookieless, script-free page-view
  and engagement counts (project clicks, résumé downloads, contact submissions)
  with daily-rotating anonymous visitor hashes — raw IP/UA never stored, raw rows
  rolled up nightly into permanent daily aggregates and deleted after 90 days,
  DNT/GPC honored; viewable at `/admin/stats`
- **Hidden admin area** at `/admin` — invisible and 404 for everyone not in
  `ADMIN_EMAILS`; sortable, filterable, paginated admin tables throughout
- **BJJ landing flavor** (opt-in): setting `SITE_FLAVOR=bjj` swaps the landing
  page into a belt-themed layout — a hero game-plan chart, a rank bar, a
  Principles section, The road (a belt ladder above a dated era table), Now
  tiles, and a two-photo hero switch — all editable at `/admin/site`;
  leave the flag unset and the default landing page renders exactly as before
- **In-app site content**: the landing page's hero heading, tagline, about,
  skills, and (under the BJJ flavor) hero eyebrow, game plan, belt caption
  and degrees, principles, eras, and Now tiles can all be overridden from
  the admin area — no redeploy needed
- **In-app theming**: every palette color (brand, dark, and light) can be
  overridden from the admin area, with swatches, hex fields, a live
  landing-page preview for either mode, and WCAG contrast warnings
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
annotated list. The `SITE_*` values seed the landing page; once running, admins can
override the hero heading, tagline, about text, and skills at `/admin/site` without
touching `.env` (blank fields fall back to the `.env` values). Under the opt-in BJJ
landing flavor (`SITE_FLAVOR=bjj`), the same admin page also carries fields for the
hero eyebrow, game plan, belt caption and degrees, principles, eras, and Now tiles.
The color palette can likewise be overridden at `/admin/theme` (blank fields fall
back to the built-in colors).

| Variable | Required | Purpose |
|---|---|---|
| `SITE_OWNER_NAME` | ✅ | Your name (hero, footer, and the nav logo's accessible label) |
| `CONTACT_EMAIL` | ✅ | Contact-form notifications + mailto links |
| `POSTGRES_PASSWORD` | ✅ | Database password (compose wires the connection string) |
| `SITE_TITLE`, `SITE_TAGLINE`, `SITE_ABOUT`, `SITE_SKILLS` | | Copy for the home page (`\n` splits paragraphs; skills comma-separated) |
| `SITE_FLAVOR` | | `bjj` (case-insensitive) opts into the belt-themed landing page below; blank or anything else renders the default landing page |
| `SITE_HERO_EYEBROW` | | Line shown above the hero heading (often your name and title); BJJ flavor only |
| `SITE_GAME_PLAN` | | Hero game-plan chart: exactly four `term \| reading \| how` lines joined with `\n`, or the chart stays hidden; BJJ flavor only |
| `SITE_BELT_CAPTION`, `SITE_BELT_DEGREES` | | Rank-bar caption and its degree stripes (0 to 6); the bar is hidden while the caption is blank; BJJ flavor only |
| `SITE_PRINCIPLES` | | Principles section: 1 to 6 `maxim \| reading` lines joined with `\n`; BJJ flavor only |
| `SITE_ERAS` | | The road's belt ladder and table: 1 to 12 `date \| belt \| stripes \| gym \| location \| role` lines joined with `\n` (`date` is `YYYY-MM-DD`; `belt` is white, blue, purple, brown, or black); BJJ flavor only |
| `SITE_NOW` | | Now tiles: 1 to 8 `label \| value` lines joined with `\n`; BJJ flavor only |
| `SITE_META_DESCRIPTION` | | Search/social snippet; blank falls back to `SITE_TAGLINE` |
| `CONTACT_PHONE`, `LINKEDIN_URL`, `GITHUB_URL` | | Contact & social links; `GITHUB_URL` drives the projects-page CTA |
| `ADMIN_EMAILS` | | Comma-separated; matching OAuth emails get the hidden admin area |
| `OAUTH__<P>__CLIENTID` / `CLIENTSECRET` | | Enable sign-in per provider (`GITHUB`, `GOOGLE`, `DISCORD`) |
| `SMTP__HOST/PORT/USER/PASSWORD/FROM` | | Email notifications; blank host = DB-only mode |
| `PUBLIC_BASE_URL` | | Canonical origin for canonical/og URLs, social cards, the feed, and the sitemap (e.g. `https://you.example`) |
| `RESUME_FILE` | | Path to a PDF served at `/resume` (with download counting); unset = no résumé link |
| `OWNER_PHOTO_FILE` | | Path to the owner photo on the landing hero, served at `/owner-photo` (see also `OWNER_PHOTO_FLIP_FILE` below); mount its folder read-write so the admin site-content page can replace it; unset = photo-less hero |
| `OWNER_PHOTO_ALT` | | Alt text for the owner photo; defaults to `Portrait of {SITE_OWNER_NAME}`, admin-overridable |
| `OWNER_PHOTO_FLIP_FILE` | | Path to a second, "mat" portrait for the hero's two-photo switch, served at `/owner-photo-flip`; BJJ flavor only, and only once `OWNER_PHOTO_FILE` is also set; unset = the hero shows just the primary photo |
| `OWNER_PHOTO_FLIP_ALT` | | Alt text for the second photo; defaults to `Portrait of {SITE_OWNER_NAME}`, admin-overridable |
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

MIT — see [`LICENSE`](LICENSE). The site ships built-in `/terms` and
`/privacy` pages that describe exactly what the software stores,
auto-personalized from your `.env`.

If this project is useful to you, you can
[buy me a coffee](https://buymeacoffee.com/jmykitta) ☕ — and self-hosters can
point the footer's sponsor link at their own page via `SPONSOR_URL` /
`SPONSOR_TEXT` (or leave it blank to hide it).
