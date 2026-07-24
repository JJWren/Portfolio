# Build and Test Summary

**Repo**: https://github.com/JJWren/Portfolio · **Stack**: Blazor Web App (.NET 10) + PostgreSQL 17 + Docker Compose

## Status at hand-off
- `dotnet build -warnaserror`: clean · `dotnet test`: **41/41 passing**
- Clean-slate compose (`down -v` → `up --build`): migrations applied, demo data seeded,
  every public endpoint 200, web + db containers **healthy**
- GitHub Actions CI: build/test + GHCR publish wired; first run triggered by the initial push
- Verified end-to-end during construction: theming/assets, blog render + 404s, comment
  visibility + moderation hide, projects carousel + visibility, contact POST (antiforgery)
  + honeypot drop, hidden admin (302 anon / 404 non-admin)

## Outstanding items that need the user (cannot be automated here)
1. **OAuth apps**: register GitHub/Google/Discord apps, put client id/secret in `.env`
   (callbacks `/signin-<provider>`), then verify live sign-in → admin role from `ADMIN_EMAILS`.
2. **SMTP creds**: set `SMTP__*` (e.g. Gmail app password) and verify contact email delivery.
3. **Personalize `.env`**: real name/tagline/about/skills/links replace the "Jane Developer"
   placeholders; set `PUBLIC_BASE_URL` when deployed (e.g. https://portfolio.theguywiththedogs.dev).
4. **Deploy**: point the reverse proxy at the container (HTTP :8080, TLS at proxy);
   archive the old Personal_Portfolio repo after go-live.

## Post-hand-off update (2026-07-24)
Items 1–4 above are complete: OAuth sign-in is live in production, `.env` is
personalized, the site is deployed behind the reverse proxy at
https://portfolio.theguywiththedogs.dev, and Personal_Portfolio is archived
(SMTP stays environment-specific; contact messages land in the DB regardless).
The test suite has since grown from 41 to 184 tests across releases
v1.1.0–v1.12.0 — see unit-test-instructions.md and CHANGELOG.md.

## Instruction documents
- build-instructions.md · unit-test-instructions.md · integration-test-instructions.md ·
  performance-test-instructions.md
