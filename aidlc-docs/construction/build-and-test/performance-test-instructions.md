# Performance Test Instructions

No formal performance requirements were set for this personal-portfolio workload; these are
sanity checks, not SLAs.

## Quick checks
- Public pages are static SSR — verify TTFB stays low locally:
  `curl -s -o NUL -w "%{time_starttransfer}\n" http://localhost:8080/` (expect well under 200 ms warm)
- Payload budget: fonts ~125 KB total, prism.js ~37 KB, no CDN requests; confirm with browser devtools.
- Blog/list queries are `AsNoTracking` with indexes on `Slug` and `BlogPostId`; verify no N+1 by
  watching `docker compose logs web` with EF command logging in Development.

## If load testing is ever wanted
`k6` or `bombardier` against `/`, `/blog`, `/blog/{slug}`; watch container CPU/memory via
`docker stats`. Interactive-server circuits (admin, comments) are the only stateful load.
