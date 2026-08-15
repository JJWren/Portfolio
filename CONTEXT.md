# Portfolio

A self-hosted personal portfolio site (Blazor + Postgres). This glossary pins
down the language of its contact-form spam defense, privacy-preserving
analytics, and blog comments.

## Language

### Site identity

**Owner Photo**:
The site owner's portrait on the landing hero — a single owner-supplied file,
swappable either by replacing the file directly or from the admin site-content
page (both paths end at the same file). When no photo is supplied the hero
simply renders without one; nothing takes its place.
_Avoid_: profile picture, avatar (those belong to user Profiles)

### Comments

**Pinned Comment**:
A comment an admin has surfaced above the regular list of a blog post. Pinned
state is independent of hidden state — hiding a pinned comment removes it from
view without unpinning it, so it returns to the pinned section when unhidden.
_Avoid_: featured comment, sticky comment, highlight

**Pinned Section**:
The labeled block of pinned comments at the top of a post's comment list,
separated from regular comments by a divider. Absent entirely — label, divider
and all — when the post has no visible pinned comments. Regular comments never
appear in it, and pinned comments never appear below it.
_Avoid_: highlights, featured section

### Contact-form spam defense

**Hard Signal**:
Mechanical proof that a submission came from a bot — the honeypot field was
filled, or the form was submitted faster than a human could read it. Hard
signals produce a Fake Success.
_Avoid_: spam score, block

**Fake Success**:
The response to a hard signal: the form shows the normal "message sent" panel
but nothing is stored or delivered, so the bot learns nothing.
_Avoid_: silent drop, rejection

**Soft Signal**:
Suspicion short of proof — a disposable sender domain, multiple links in the
body, a URL in the subject, or an unreadable render token. Soft signals
quarantine; they never discard.
_Avoid_: spam filter, blacklist match

**Quarantined Message**:
A contact message flagged by a soft signal: stored and reviewable in the admin
inbox with its flag reason, but no notification email is sent and it stays out
of the unread attention counts. An admin can clear the flag ("Not spam").
_Avoid_: spam folder, blocked message

**Render Token**:
The tamper-proof timestamp issued when the contact form is rendered and posted
back with it, proving how long the visitor took to submit.

**Disposable Domain**:
An email domain from the vendored burner-address blocklist. A sender using one
is a soft signal.
_Avoid_: fake email, throwaway

### Analytics

**Visitor Key**:
The anonymous daily identifier: a one-way keyed hash of the per-install
secret, the UTC date, the client IP, and the User-Agent. The raw inputs are
never stored, and the embedded date makes keys unlinkable across days.
_Avoid_: user id, fingerprint, session id

**Daily Visitor**:
One distinct Visitor Key on one UTC day. The site's only uniqueness metric —
cross-day uniques are intentionally impossible.
_Avoid_: unique visitor, returning visitor

**Named Event**:
A counted engagement action: `project-click`, `resume-download`, or
`contact-submit`.
_Avoid_: goal, conversion

**Rollup**:
The nightly aggregation of raw page views and events into permanent daily
totals, after which raw rows older than the 90-day retention period are
deleted.

**Watermark**:
The latest day present in the daily site stats — every day up to it has been
rolled up (zero-traffic days included), and the next rollup resumes after it.
