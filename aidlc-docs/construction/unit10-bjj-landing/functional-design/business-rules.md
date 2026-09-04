# Unit 10 — Functional Design: business rules

**Written**: 2026-09-04. Rules are numbered so the plan, the tests and the audit can cite them. Every rule below is pure logic in `Services/` (testable without EF) or a rendering rule pinned by `LandingSectionsRenderTests`.

## Flavor and visibility

- **BR-1 Flavor gate.** The BJJ pieces (eyebrow, game-plan chart, rank bar, Principles, The road, Now, portrait switch) render only when `SiteConfig.Flavor == Bjj`. Under `Default` the landing page renders exactly as v1.22.0, whatever the new columns hold. The `/admin/site` editor shows the BJJ fields only under `Bjj` so a plain install never sees them.
- **BR-2 Blank-hides.** Each BJJ section hides independently when its resolved data is empty: no eyebrow, no chart, no rank bar, no Principles, no road, no Now. The page must look finished with any subset present.
- **BR-3 Resolution precedence.** Per field: admin override, then env value, then hidden. Identical to `Tagline`, `About` and `Skills`; an empty text[] counts as "not overridden".

## Parsing and validation

- **BR-4 Lenient at resolve, strict at save.** `Resolve` drops malformed lines silently so a bad env value can never take the landing page down (the DB-blip rule in `SiteContentService` already serves env defaults on failure). The editor's `Validate(draft)` reports the first problem with a friendly message ("Eras line 3: belt must be white, blue, purple, brown or black") and refuses the save.
- **BR-5 Game plan.** Exactly four lines or none. Fields: `term` and `reading` required, `how` may be blank. Node colors are positional: red, gold, green, blue (the four brand tokens, so the admin theme still recolors them). Every node is one link to `#principles` (decision 4).
- **BR-6 Rank bar.** Renders when `BeltCaption` is non-empty. Drawn as a black belt (`--belt-black` body ringed with `--border`, red bar, black tip) with `BeltDegrees` stripes, 0 to 6; values outside the range are rejected at save and clamped at resolve. v1 limitation, recorded: the bar is always a black belt; a "current belt" field is a follow-up if a non-black owner ever adopts the flavor.
- **BR-7 Principles.** One to six `maxim | reading` lines; `maxim` required, `reading` may be blank. The grid wraps at three per row.
- **BR-8 Eras.** One to twelve lines in the order entered (chronological order is the owner's job; nothing re-sorts). `date` must parse as `YYYY-MM-DD`; `belt` must be in the closed set; `stripes` is 0 to 6 (blank = 0); `gym`, `location` and `role` may be blank (empty cell). The ladder shows one rung per distinct belt in first-appearance order, carrying the stripes of the last era of that belt; the table shows every era as a row with `data-belt`.
- **BR-9 Degrees consistency.** When a black-belt era exists and `BeltDegrees` is set, the era's stripes must equal `BeltDegrees`; the editor refuses the save otherwise ("Belt degrees (0) and the black belt era's stripes (1) disagree"). One fact in two places, kept equal at the only place it can drift.
- **BR-10 Now.** One to eight `label | value` lines; `label` required, `value` may be blank.
- **BR-11 Lengths.** `HeroEyebrow` 120, `BeltCaption` 200, `OwnerPhotoFlipAlt` 200, every text[] line 500 characters. Limits live as constants in `SiteContentRules` (the AppDbContext max-length pattern); the multi-line textareas carry no `maxlength` because they hold many lines.

## Rendering invariants (pinned by tests)

- **BR-12 Static SSR.** `LandingSections` and its children take no `@rendermode`; interactivity is CSS (`:has()`, `:hover`, `:focus-visible`, scroll timelines) only. No new JS on `/`.
- **BR-13 Preview-safe.** Nothing inside `LandingSections` is `position: fixed`; every interactive device is complete at rest (desk radio checked, chart nodes plain, road readable without hover). Pinned by `LandingSectionsRenderTests` (rendered HTML plus a scan of `app.css` fixed-position selectors) and `AppCssTests`.
- **BR-14 Motion.** Every new `animation` and `transition` in the landing CSS is listed in the single `prefers-reduced-motion` block of `app.css`; the mobile scroll glow lives under `@supports (animation-timeline: view())` with the static row as the fallback. Timeline names are index-based (`era-1` to `era-8`) so two eras on the same belt never share a timeline; rows past eight stay static.
- **BR-15 Fixed constants.** Belt and rank colors are `:root` constants (ADR 0002), never theme tokens; the 26-token catalog and its pinned tests do not change. The preview frame inherits them from `:root`.
- **BR-16 Accessibility.** Belt red never carries text. The ladder is `aria-hidden` (the table carries the information). The table's band column is a plain empty header cell, hidden with `display: none` on desktop and shown on mobile with the band span itself `aria-hidden`; no `aria-hidden` on table cells (a small correction to the canvas markup so assistive tech sees a consistent column count). Dates render as `<time datetime="YYYY-MM-DD">` in ISO form (the design's mono style), not localized.
- **BR-17 Portrait switch.** Two radios in a fieldset with a visually hidden legend; the desk radio is checked by default; the crossfade is 0.25s and none under reduced motion. Renders only when both slots resolve (see domain-entities.md section 4).
- **BR-18 Owner facts.** No owner fact lives in code, tests or `.env.example`; they are content the owner enters at `/admin/site` (or in the production `.env`). Tests use neutral fixtures. The exact strings are kept in the plan's content sheet.
- **BR-19 SEO unchanged.** `<title>`, `og:title`, the meta description and the JSON-LD `Person` keep the owner name and the primary photo (decision 5).
