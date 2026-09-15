# Security Verification Instructions

Unit 12a (`feat/security-headers`) adds security response headers and an
enforced Content-Security-Policy; Unit 12b (`feat/rate-limiting`) adds
framework rate limiting on auth, feeds and redirects, the generalized
circuit limiter for comments and reports, and `TRUSTED_PROXIES` for the
forwarded-for boundary. This document is the checklist for verifying both:
locally with curl and a browser before each PR, and in production after
each deploy.

## Local setup (the Unit 11 recipe)

Run the app against an ephemeral Postgres on a throwaway port so nothing
touches a real database:

```bash
docker run -d --name portfolio-csp-check -p 55432:5432 \
  -e POSTGRES_USER=portfolio -e POSTGRES_PASSWORD=placeholder -e POSTGRES_DB=portfolio \
  postgres:17-alpine
# wait for it to accept connections
```

Then, exported for that process only:

```bash
ConnectionStrings__Default="Host=localhost;Port=55432;Database=portfolio;Username=portfolio;Password=placeholder" \
ASPNETCORE_URLS=http://localhost:5199 \
ASPNETCORE_ENVIRONMENT=Development \
DataProtection__KeysPath=/tmp/portfolio-csp-dpkeys \
SITE_OWNER_NAME="Jane Developer" \
CONTACT_EMAIL="jane@example.com" \
SEED_DEMO_DATA=true \
SITE_FLAVOR=bjj \
dotnet run --project src/Portfolio.Web
```

`SEED_DEMO_DATA=true` gives `DemoSeeder`'s two posts, each with a fenced
code block (for Prism/`script-src`); it does not create any file under
`/uploads` or any comment (both need an OAuth sign-in this recipe doesn't
have), so the `/uploads/...` curl check and the comment-section browser walk
below are as far as this recipe alone can reach — see the notes at each
step for how to go further.

Run once with `SITE_FLAVOR=bjj` (a belt caption on the landing page) and
once with `SITE_FLAVOR` unset (the plain landing page), per FR-D8.

Stop the app by PID (never `taskkill /IM dotnet.exe`) and remove the
container (`docker rm -f portfolio-csp-check`) when done with every step
below.

## 1. Headers with curl

With the app running (`SECURITY_CSP_MODE` unset, so `enforce`):

```bash
curl -sI http://localhost:5199/
curl -sI http://localhost:5199/no-such-page
curl -sI http://localhost:5199/feed.xml
```

Every response must carry, exactly. The `Permissions-Policy` and
`Content-Security-Policy` values below are copied from `SecurityHeadersRules`
for this curl comparison, are pinned by `SecurityHeadersRulesTests`, and must
be updated together with that test:

- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-Frame-Options: DENY`
- `Permissions-Policy: accelerometer=(), autoplay=(), browsing-topics=(), camera=(), clipboard-read=(), clipboard-write=(), display-capture=(), fullscreen=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), picture-in-picture=(), screen-wake-lock=(), usb=(), web-share=(), xr-spatial-tracking=()`
- `Cross-Origin-Opener-Policy: same-origin`
- `X-DNS-Prefetch-Control: off`
- `X-Permitted-Cross-Domain-Policies: none`
- `Content-Security-Policy: default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self'; img-src 'self' blob: https:; font-src 'self'; connect-src 'self'; manifest-src 'self'` (the `style-src` grows a `'sha256-...'` entry only once an override is saved — section 3)

And no response should carry a `Server` header.

If the demo seeder or an admin upload has stored a file, repeat the first
command against `/uploads/<filename>`: it must carry
`Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; sandbox`
and `X-Content-Type-Options: nosniff` instead of the set above (the uploads
policy, set earlier by the static-file middleware, wins over the page
policy) — still no `Server` header, and still `Cache-Control: public,
max-age=31536000, immutable`. With nothing seeded under `/uploads`, this
part of the check is only reachable by an admin (OAuth sign-in required):
production, or a local run with a real provider configured.

## 2. The CSP in a browser, enforce (the default)

With the app running and the browser's console open, visit each of the
FR-D8 pages, moving between most of them by clicking links (Blazor's
enhanced navigation) rather than typing the URL every time, and read the
console after each for CSP violation reports (there must be zero):

1. `/` — both with `SITE_FLAVOR=bjj` and with it unset (two separate runs).
2. A blog post with a fenced code block (`/blog/welcome` or
   `/blog/markdown-tour` from the demo data) — confirms Prism highlighting
   works under `script-src 'self'`.
3. The same post's comment section — the interactive circuit and its
   markdown preview. Posting a comment needs a signed-in OAuth account,
   which this local recipe doesn't have; loading the section and typing in
   its textarea (without submitting) still exercises the circuit and the
   markdown preview under the policy.
4. `/contact` — fill in and submit the form with invented values (never
   real personal data).
5. `/signin` — the provider-picker page (no need to complete an OAuth
   round trip; the page itself is what's under test).
6. `/feed.xml`.
7. `/no-such-page` — the re-executed 404.

Then reload one page with a full browser refresh (not a link click) and
confirm the console is still clean afterward.

Record, for each page: visited (yes/no) and console violations (count,
ideally zero).

## 3. The hash path (theme override), enforce

This exercises `ThemeRules.StyleHash` and the middleware's fill of
`style-src` end to end, without needing the admin UI:

```bash
docker exec -it portfolio-csp-check psql -U portfolio -d portfolio -c \
  "INSERT INTO \"ThemeSettings\" (\"Id\", \"Overrides\", \"UpdatedAt\") VALUES (1, '{\"dark-bg\":\"#0a5c36\"}'::jsonb, now()) ON CONFLICT (\"Id\") DO UPDATE SET \"Overrides\" = EXCLUDED.\"Overrides\", \"UpdatedAt\" = EXCLUDED.\"UpdatedAt\";"
```

Then reload `/` in the browser (a full reload, so the new snapshot and
header arrive together — see ADR 0003) and confirm:

- The page renders with the overridden dark background.
- `curl -sI http://localhost:5199/` shows `style-src 'self' 'sha256-...'`
  in the `Content-Security-Policy` header (a real hash, not a placeholder).
- The console shows zero CSP violations — the override `<style>` block in
  `<head>` is admitted by that exact hash.

## 4. The owner's production checklist, report-only

The admin pages (OAuth-gated) cannot be driven by this local recipe, so
they are checked in production, right after the deploy, with
`SECURITY_CSP_MODE=report-only` set (nothing is blocked; the console still
reports what the enforced policy would have stopped). With the console
open on each page:

- `/admin/theme` — the swatches (color picker open/apply), the preview's
  mode toggle, and a save (confirms the forced full-page reload and the
  new override block/hash arriving together).
- `/admin/site` — an image upload and the crop editor.
- `/admin/stats`.
- `/admin/posts`.

Zero CSP reports on all four before moving on.

## 5. The flip to enforce

Once the report-only checklist above is clean: set
`SECURITY_CSP_MODE=enforce` (or remove the variable — `enforce` is the
default) in the production `.env`, recreate the container, and repeat the
`curl -sI` checks of section 1 against the production origin to confirm
`Content-Security-Policy` (not `-Report-Only`) is now sent.

## 6. Rollback

If something in production needs the policy relaxed or removed without a
code change: set `SECURITY_CSP_MODE=report-only` (log without blocking) or
`SECURITY_CSP_MODE=off` (send no Content-Security-Policy header at all) in
`.env` and recreate the container. The other security headers (section 1)
are unaffected by this setting and keep being sent either way.

## 7. The 429 walk-through (rate limiting, Unit 12b)

With the app running (the same local setup as above; `TRUSTED_PROXIES`
unset so the loopback connection keys its own bucket), loop 31 requests to
`/feed.xml` inside a minute:

```bash
for i in $(seq 1 31); do
  curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5199/feed.xml
done
```

The first 30 answer `200`; the 31st answers `429`. Confirm the last
response's headers and body:

```bash
curl -si http://localhost:5199/feed.xml
```

carries `Retry-After: <N>` (whole seconds, at least 1) and a
`text/plain; charset=utf-8` body reading exactly `Too many requests. Try
again in <N> seconds.`. Waiting out the window and repeating the single
`curl -si` call answers `200` again.

Throughout the loop, `curl -s -o /dev/null -w "%{http_code}\n"
http://localhost:5199/healthz` must keep answering `200` — `/healthz` is
never limited.

The comment-posting and report-submission limits (FR-D12) cannot be
exercised locally without a signed-in OAuth account; `CommentServiceTests`
and `ReportServiceTests` (database-free, over a throwing
`IDbContextFactory`) carry that coverage instead — see
`unit-test-instructions.md`.

Record the loop's status-code sequence and the 429 response's headers/body
here once run.

## 8. The proxy-trust check (`TRUSTED_PROXIES`, Unit 12b)

With `TRUSTED_PROXIES` unset (today's behaviour, every peer trusted), a
direct request carrying a forged `X-Forwarded-For` shares the limit bucket
of that forged address:

```bash
curl -s -o /dev/null -w "%{http_code}\n" -H "X-Forwarded-For: 203.0.113.9" http://localhost:5199/feed.xml
```

repeated past the feed's 30/minute limit ends in `429` for that forged
address specifically — a second run using a different forged address
starts its own fresh bucket.

Restart the app with `TRUSTED_PROXIES=192.0.2.0/24` (a documentation range
that does not include the test client's own loopback address) and repeat
the forged-header request: the connection address is used instead, so the
forged `X-Forwarded-For` no longer moves the bucket the request lands in.
Record both runs' results here.

Stop the app by PID (never `taskkill /IM dotnet.exe`) and remove the
container (`docker rm -f portfolio-csp-check`) when done — the same
housekeeping "Local setup" above asks for, now covering sections 7 and 8
too.
