# First-party cookieless analytics with daily-rotating visitor keys

The site needed visitor and engagement stats without collecting personal data.
We record page views server-side in the app itself (middleware into Postgres,
viewed at /admin/stats) instead of embedding a third-party or self-hosted
analytics service, and we identify visitors by a keyed hash of
secret + UTC date + IP + User-Agent rather than a cookie or stored IP.

## Considered Options

- **Umami/Plausible container + JS snippet** — rejected: an extra service to
  run, a script for ad-blockers to eat, and the privacy guarantee lives in a
  vendor's defaults instead of our own code.
- **Anonymous cookie ID** — rejected: accurate cross-day uniques, but it's a
  client-side identifier and drags the site into consent-banner territory.

## Consequences

- Cross-day unique visitors are **intentionally impossible**: the date is part
  of the hash, so "visitors" always means daily visitors summed over a period.
- No script, no cookie: the Privacy page's "nothing runs in your browser"
  claims stay literally true; outbound project-link clicks are counted via the
  first-party `/go` redirect endpoint instead of JS.
- Raw per-view rows are deleted after 90 days by the nightly rollup; only
  daily aggregates are permanent. DNT/Sec-GPC visitors are never recorded.
