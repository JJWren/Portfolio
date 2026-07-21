# Integration Test Instructions

Run against the compose stack: `docker compose up -d --build` (fresh state: `docker compose down -v` first).

## Automated-style HTTP checks (performed during construction; repeatable)
| Area | Check |
|---|---|
| Boot | `GET /` → 200; web + db containers report healthy (`docker compose ps`) |
| Health | `GET /healthz` → 200 (includes DB probe) |
| Theming | Page HTML contains `theme.js`, ribbons, footer strip; fonts + css + js assets 200 |
| Blog | `/blog` lists published posts w/ reading time; `/blog/{slug}` renders markdown + `language-*` code class; unknown slug → 404 |
| Comments | Seeded comment renders w/ author; `IsHidden=true` removes it publicly |
| Projects | Visible cards render w/ links; hidden project excluded; GitHub CTA + carousel controls present |
| Contact | POST w/ antiforgery token → row in `ContactMessages` + success state; honeypot-filled POST → fake success, no row |
| Auth | `/signin` 200; anonymous `/admin` → 302 `/signin`; no Admin nav leak |
| SEO | `/feed.xml`, `/sitemap.xml`, `/robots.txt` → 200 with expected content |
| Seeding | `SEED_DEMO_DATA=true` on wiped volumes populates posts + projects |

## Requires user-supplied OAuth credentials (manual)
1. Register apps (callbacks `/signin-github`, `/signin-google`, `/signin-discord`), fill `OAUTH__*` in `.env`, restart.
2. Sign in with an `ADMIN_EMAILS` account → Admin nav appears; full post/project CRUD via UI; image upload persists in `uploads` volume.
3. Sign in with a non-admin account → no Admin nav; `/admin` → 404; can comment and delete own comment.
4. With real `SMTP__*` creds: contact submission arrives by email with visitor as Reply-To.
