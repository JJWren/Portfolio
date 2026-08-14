# Unit 9 — Contact-Form Spam Hardening + First-Party Analytics

Design settled 2026-08-14 via grilling session; full plan in the session plan
file, durable decisions in `CONTEXT.md` and `docs/adr/0001`.

## Design decisions (user-approved)

- Bots are blocked mechanically; human sales mail is quarantined for review,
  never hard-blocked. Fully self-contained — no CAPTCHA/third-party services.
- Hard signals (honeypot, <4s submit) → silent fake success. Soft signals
  (disposable domain, ≥2 body links, subject URL, unreadable render token) →
  quarantine: stored + reviewable, no SMTP, excluded from attention badges.
- Fake emails: MX/DNS validation (fail-open, ~2.5s cap) + vendored
  disposable-domain blocklist. No confirmation-link loop.
- Analytics: first-party, server-side, cookieless, script-free. Daily-rotating
  HMAC visitor keys (secret + UTC date + IP + UA; raw values never stored).
  Bots-by-UA, Admin sessions, and DNT/Sec-GPC visitors excluded entirely.
  Raw rows 90 days → permanent daily aggregates (nightly 00:20 UTC rollup,
  the app's first hosted service). Privacy page reworded to match.

## Increments

- [x] Increment 1 — spam hardening (rules, timestamp token, blocklist, MX
      checker, ContactMessage flagging + migration `AddContactMessageFlagging`,
      inbox Flagged filter + Not-spam action, dashboard badge)
- [x] Increment 2 — analytics capture (entities + migration
      `AddAnalyticsCapture`, VisitorKey, AnalyticsRules, middleware, `/go` +
      `/resume` endpoints, link rewiring, contact-submit event)
- [x] Increment 3 — rollup + retention (aggregate entities + migration
      `AddAnalyticsRollups`, AnalyticsRollup pure logic, AnalyticsRollupService
      BackgroundService with startup catch-up + idempotent per-day rollup)
- [x] Increment 4 — `/admin/stats` page + Dashboard card, Privacy rewording,
      README/.env.example `RESUME_FILE`, CONTEXT.md, ADR 0001
- [x] Tests: 390/390 (+48 pure-logic tests across 8 suites)
- [x] Live smoke test vs throwaway Postgres — caught and fixed two prod-only
      bugs (DI IEnumerable<string> ctor selection → empty blocklist; Npgsql
      rejecting Kind=Unspecified DateTimes in rollup/stats queries); all spam
      dispositions, analytics exclusions, /go redirect, and rollup catch-up
      verified against the real database (see audit.md addendum)
- [ ] PR opened and Copilot review gate cleared
