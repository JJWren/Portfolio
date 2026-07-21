# Unit 7 Plan — Contact Form (SMTP + DB + Admin Inbox)

Static-SSR form (no websocket needed to say hello). Every message is stored in the DB;
email delivery via MailKit is best-effort on top (failure logged, message never lost).
Spam defenses: honeypot field (silent drop) + per-IP fixed-window rate limit.

- [x] ContactMessage entity (name, email, subject, body ≤4000, ReceivedAt, IsRead) + migration
- [x] MailKit 4.17; EmailService (env-config SMTP, no-op when SMTP__HOST blank, replyTo visitor,
      failures logged — message always stored first)
- [x] ContactRateLimiter (per-IP fixed window, 3/10min, TimeProvider) + 3 tests (41 total green)
- [x] ContactService (save + best-effort email, admin list, mark read/unread, delete)
- [x] /contact page: SSR EditForm (DataAnnotations validation, friendly messages), honeypot,
      rate-limit message, success state, contact details aside from SiteConfig
- [x] Admin /admin/messages inbox (InteractiveServer): unread badges, expand body,
      mark read/unread, reply-by-email mailto, delete
- [x] Dashboard messages card links to inbox
- [x] Styles on existing tokens (contact grid, honeypot hiding, inbox rows)
- [x] Verify: compose: real antiforgery POST saved to DB + success state; honeypot POST returned
      fake success with no DB row. SMTP send untested (placeholder creds) — fails soft by design.
