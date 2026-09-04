# Portfolio

A self-hosted personal portfolio site (Blazor + Postgres). This glossary pins
down the language of its contact-form spam defense, privacy-preserving
analytics, and blog comments.

## Language

### Site identity

**Owner Photo**:
The site owner's portrait on the landing hero — up to two owner-supplied
files, each swappable either by replacing its file directly or from the
admin site-content page (both paths end at the same file). The primary
(desk) photo renders alone by default; add the second, optional Flip photo
(the mat portrait) and, under the Bjj flavor, the hero instead shows both
in a switch the visitor can toggle, served at `/owner-photo` and
`/owner-photo-flip`. With no file supplied for a slot, that slot simply
renders nothing; nothing takes its place.
_Avoid_: profile picture, avatar (those belong to user Profiles)

### Landing page (BJJ flavor)

**Flavor**:
The `SITE_FLAVOR` switch between the site's two landing-page layouts —
Default (today's plain landing page) and Bjj (the belt-themed layout
below). Startup configuration only, never admin-editable: it decides which
markup and CSS ship, not which words appear.
_Avoid_: theme, skin, mode

**Game Plan**:
The four-node chart in the Bjj hero — Guard, Pass, Mount, Submit — each one
a link down to the principles section. Positionally colored regardless of
its own wording, so the admin theme's brand colors keep recoloring it.
_Avoid_: roadmap, funnel

**Rank Bar**:
The belt-and-stripes graphic on the hero/About seam, captioned with the
owner's rank. Always drawn as a black belt in v1, carrying the configured
count of degree stripes.
_Avoid_: progress bar

**Belt**:
One of the five ranks the site knows — white, blue, purple, brown, black —
with colors fixed by ADR 0002, never by the admin theme. A value outside
this closed set is rejected at save and dropped at render.
_Avoid_: rank color, level

**Degree**:
One stripe earned on the black belt, 0 to 6. Kept equal to the stripe count
of the last black-belt era — one fact recorded in two places, checked
against itself at save.
_Avoid_: dan, level

**Era**:
One row of the road: the date a belt was earned, the gym and location, and
what the owner was doing at the time. Eras render in the order entered;
nothing re-sorts them.
_Avoid_: milestone, timeline entry

**The Road**:
The Bjj landing section pairing a belt ladder — one rung per belt reached —
with a dated table of every era. The ladder is decorative; the table alone
carries the information for assistive tech.
_Avoid_: history, career timeline

**Now**:
The label/value tiles closing the Bjj landing page: a short, current list
of what the owner is doing today.
_Avoid_: status, currently

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
