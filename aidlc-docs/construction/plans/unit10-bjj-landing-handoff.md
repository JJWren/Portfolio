# Unit 10 — BJJ landing page: handoff for the next session

**Written**: 2026-09-04. **Baseline**: `master` @ `e1825d9` (v1.22.0), working tree has uncommitted docs only (see "Repo state").
**Where the workflow stands**: Inception is complete for this unit (Reverse Engineering, Requirements Analysis, Application Design). The next session starts **Construction → Unit 10**, beginning with the Code Generation plan (Part 1: a checkbox plan presented for approval) after a short Functional Design pass for the new data fields. Log every owner input in `aidlc-docs/audit.md` and keep `aidlc-docs/aidlc-state.md` current.

## Links

| What | Where |
|---|---|
| Design canvas (the approved design; two pages, saving enabled) | https://claude.ai/code/artifact/4e3d73fd-098f-472f-a3c9-0678be44349c |
| Design-system project in Claude Design ("Portfolio Design System") | https://claude.ai/design/p/0eace0af-92bf-4fa4-b035-9fa3dd8dab6d |
| Live site | https://joshuamykitta.dev |
| Canvas sources (re-seedable) | `aidlc-docs/inception/application-design/landing-directions/` (`parts/`, `build.mjs`, `canvas.json`, seven `.dc.html`, images, `tools/`) |
| Review docs | `aidlc-docs/inception/reverse-engineering/*.md` (eight files) |
| Requirements and decisions | `aidlc-docs/inception/requirements/requirements.md` (sections 5, 9, 11) |

## The approved design (page 1 "Lead", artboard `Main.dc.html`), top to bottom

1. **Header**: unchanged from the live site (logo, Projects / Blog / Contact, sign-in, theme toggle). Toggle `title` may read "Switch to the white gi (light theme)" / "Switch to the black belt (dark theme)"; the accessible name stays functional.
2. **Hero**, a two-column grid (`minmax(0,1fr) 320px`, the existing `.has-photo` rules):
   - Left: eyebrow `JOSHUA MYKITTA · SOFTWARE ENGINEER`, h1 **"Position before submission."**, tagline "A game plan borrowed from the mat: secure the application first, build the tooling that improves position, then finish. Secure software engineer; Brazilian jiu-jitsu black belt." (draft, owner may rewrite), then the existing CTA row (GitHub primary, LinkedIn ghost, Get in touch ghost).
   - Right: the portrait in the 4:5 slot with a two-radio pill switch under it, `AT THE DESK` / `ON THE MAT` (CSS `:has(:checked)` crossfade, 0.25s, none under reduced motion). Desk photo: `josh_profile.JPG`, alt "Joshua Mykitta in sunglasses and a tuxedo, looking slightly off to the left with a subtle, pensive smile". Mat photo: `Promotion-black-belt-solo.jpg`, alt "Joshua Mykitta, on the left, being promoted to Brazilian jiu-jitsu black belt by Rodney Souza, on the right".
   - Below both columns (`grid-column: 1 / -1`): the **game-plan chart**, four cards with a colored top border and arrow connectors: Guard (red) "Secure the position" / "Auth, secrets, boundaries. Get the position first."; Pass (gold) "Improve the position" / "Developer tooling that makes the next move easier."; Mount (green) "Keep control" / "Tests, reviews, boring deploys."; Submit (blue) "Finish" / "Ship it, then write it up." Hover or focus a node: its border and outgoing arrow take the node color and the next node lifts. Every node links to `#principles`. Readings are drafts.
   - The four ribbons stay exactly as today.
3. **Rank bar** on the hero/About seam: a 12px belt across the container (`--belt-black` body ringed with `--border`, a 112px red bar carrying the degree stripes, a black tip), caption in the eyebrow style: "Black belt · Championship Mixed Martial Arts, Daphne, AL · promoted December 9, 2025 by Rodney Souza". Degrees today: **0** (no stripes). Stripes, when they exist, wrap on left-to-right at load (0.5s each, staggered; none under reduced motion).
4. **About**: exactly the live site's three paragraphs and seven skill chips (already served from `/admin/site`); no change to markup.
5. **Principles** (`id="principles"`), three pairs in a 3-column grid (1 column under 720px): "Position before submission." / "Security before features. Lock the position down, then attack the problem."; "Tap early, tap often." / "Roll back early. A revert costs nothing; an outage does."; "Leave your ego at the door." / "Code review is not a fight. The best idea wins, not the loudest." Readings are drafts.
6. **The road** (eyebrow "The road", h2 "Two ladders, one clock"):
   - Desktop: a five-band **belt ladder**, white → black left to right (`repeat(5, 1fr)`, 16px bands, true belt colors ringed with `--border`, mono uppercase names under each). Colored belts carry a black bar with the stripes earned (white 2, blue 3, purple 1, brown 4); the black belt carries the red bar with the degree stripes (0). Below it a **five-column table**: Date (YYYY-MM-DD), Belt (swatch + name), Gym, Location, Role. Headers in the admin-table mono uppercase style.
   - **Hover link (desktop)**: hovering a band gives it the accent ring (`outline: 2px`, offset 3px) plus a soft accent glow and tints its row (`color-mix(accent 9%)`, swatch ringed, role text brightened); hovering a row glows its belt back. Pure CSS via `:has()`. Every row is fully readable at rest; the glow is decoration only.
   - **Under 720px**: the ladder is hidden; each row stacks into a labelled block (Date, Belt, Gym, Location, Role) with **its own belt band above it**; the row passing through the middle of the screen glows with its belt via a CSS scroll timeline (`view-timeline-name` per row, `timeline-scope` on the table, `@supports (animation-timeline: view())`, static otherwise).
7. **Now**, four surface tiles: Teaches "Adult no-gi, Tuesday and Thursday mornings, at Championship Mixed Martial Arts"; Building "CalCrony: a self-hosted Discord bot for events and RSVPs, with reminders, ICS feeds and Google Calendar free/busy checks. The .NET 10 API is the product; the bot is just a client." (draft from his repo description); Home lab "Containerized, locked down, and built for high availability. This site runs on it." (draft from his Home-Lab repo description); Household "Wife, 5 kids, 6 dogs".
8. **Footer**: unchanged.

Exact CSS for every piece is in `landing-directions/parts/` (`shared.css` mirrors `app.css`; `Main.css`, `GamePlan.css`, `LongRoad.css`, `Lead.css` hold the new blocks). Copy values from there, not from memory.

## The Road data (final, from the owner)

| Date | Belt | Gym | Location | Role |
|---|---|---|---|---|
| 2005-12-01 | White (2 stripes earned) | Tamaso's MMA | Fairhope, AL | High school junior, working at Baumhower's Wings. |
| 2018-01-30 | Blue (3) | Bagram BJJ | Bagram, Afghanistan | Veteran and contractor in Afghanistan, keeping Army helicopters in top shape as an aviation backshop mechanic. |
| 2019-08-23 | Purple (1) | Bagram BJJ | Bagram, Afghanistan | Backshop manager leading multiple shops and 20+ personnel, and teaching BJJ to base personnel at night. |
| 2020-09-23 | Brown (4) | Iron Grip BJJ | Fairhope, AL | Transitioning into tech through a University of Arizona Cyber Operations degree and the Coding Dojo coding bootcamp. |
| 2025-12-09 | Black (0 degrees) | Championship Mixed Martial Arts | Daphne, AL | Software Engineer at Acentra Health, building healthcare software that integrates care management, utilization management, and prior authorization requests into a single system. |

Other facts: on the mat since December 2005; building software since January 2021; promoted by Rodney Souza. The academy's name is "Championship Mixed Martial Arts" (confirmed 2026-09-04).

## Constraints that govern the implementation (verified in the reverse-engineering pass)

- Public pages stay static SSR. No `InteractiveServer` on `/`. Interactivity is CSS or a few lines of first-party JS in the `wwwroot/js/site.js` pattern; no inline `onclick` additions.
- `Components/LandingSections.razor` renders on `/` and inside the inert, half-width admin theme preview (`Components/Admin/ThemeEditor.razor:118`). Nothing `position: fixed` inside it; every interactive element must look finished at rest (the portrait switch's default radio, the table with no hover).
- README's "same image works for anyone": owner-specific UI is data-driven or opt-in. Recommended `SITE_FLAVOR=bjj` env switch parsed in `Services/SiteConfig.cs`; blank-hides for every new field.
- Belt and rank colors are fixed `:root` constants (`--belt-black #0c0c0c`, `--belt-white #e8e4dd`, `--rank-white #e6dfd0`, `--rank-blue #2b4c8c`, `--rank-purple #5a3d8a`, `--rank-brown #6b4423`, `--rank-black #0c0c0c`), not admin tokens; the 26-token catalog and its pinned tests stay untouched. Record as `docs/adr/0002-belt-colors-are-fixed-constants.md`.
- Every new animation joins the single reduced-motion block (`app.css:282-285`); scroll-driven effects are `@supports`-gated with a finished static fallback (Firefox stable lacks them).
- Belt red never carries text (about 2.9:1 on dark). Fraunces has no italic face.
- No CDN, no third-party scripts, no emoji; icons are inline SVG.
- One PR per phase sized for the Copilot review gate; conventional-commit titles from the CI allow-list; `dotnet build -warnaserror` and `dotnet test` green.

## Decisions locked with the owner (2026-09-04)

1. **`SITE_FLAVOR=bjj`** env switch parsed in `Services/SiteConfig.cs`; nothing hardcoded; unknown values fall back to the plain landing page.
2. **New copy lives in `SiteContent` columns** edited at `/admin/site` with env fallbacks: hero eyebrow, chart nodes (term, reading, how, four of each), belt caption and degrees, principles (three pairs), eras as `text[]` lines `date | belt | stripes | gym | location | role`, now tiles (four label/value pairs). The tagline stays in `Tagline`.
3. **Second portrait through a second `OwnerPhotoService` slot** (`OWNER_PHOTO_FLIP_FILE`, `OWNER_PHOTO_FLIP_ALT`, `/owner-photo-flip`, alt column + migration), uploadable from `/admin/site`.
4. **Chart nodes link to `#principles`** for v1: every node is a real, keyboard-focusable link and a click scrolls to the Principles section, which is the plan in words. Per-node sections (Position / Transition / Control / Finish) can come later if the owner writes their copy; a link-free graphic was the other option and was not chosen.
5. **Hero h1 is "Position before submission."** with the name in the eyebrow. `<title>`, the JSON-LD `Person` name and `og:title` keep the name, so search results are unaffected.
6. **The two portrait JPEGs may be committed** with the docs.
7. **The academy is "Championship Mixed Martial Arts"** everywhere (caption, road table, Now tile, Mat Room copy, docs).

## Recommended phases (one PR each; details and file lists in `requirements.md` section 9 and the feasibility notes in `code-quality-assessment.md`)

- [ ] **Phase 0, no code**: unpublish or replace the leftover demo post "A quick markdown tour" on `/blog`; optionally align the local `.env` with production (`PUBLIC_BASE_URL`, tagline, skills, `SEED_DEMO_DATA=false`). Everything else on the original checklist turned out to be set in production already (canonical, meta description, résumé, portrait), verified live on 2026-09-04.
- [ ] **Phase 1, `test:`**: `tests/Portfolio.Tests/LandingSectionsRenderTests.cs` using `HtmlRenderer` (no new package): h1, tagline, About paragraph count, skills, `has-photo`, and the invariant that nothing fixed-positioned renders. Delete `UnitTest1.cs`. Correct the stale counts in `construction/build-and-test/unit-test-instructions.md`.
- [ ] **Phase 2, `feat:` foundation**: `SITE_FLAVOR`, belt constants in `app.css`, ADR 0002, `.visually-hidden` utility, the hero restructure (eyebrow, h1, tagline, CTAs, portrait switch, chart component `Components/GamePlan.razor`), the rank bar, Principles; `SiteContent` columns + migration + `/admin/site` fields; render tests extended.
- [ ] **Phase 3, `feat:` The road + Now**: `SiteContent.Eras` (`text[]`) and Now lines + migration + editor; the ladder, table, hover CSS, mobile bands and scroll glow; render tests for five rows.
- [ ] **Phase 4, `feat:` second portrait** (if decision 3 is the service slot): `OwnerPhotoService` slot, endpoint, alt column, editor upload; `fetchpriority` on the hero image.
- [ ] **Phase 5, `docs:`/`perf:` close-out**: README features and env table, `CONTEXT.md` glossary (Belt, Degree, Era, Flavor), font preload for Fraunces, replace the four inline `onclick` handlers, `aidlc-state.md`, `audit.md`.

## Re-seeding the canvas after a design change

1. Edit under `landing-directions/parts/`, run `node build.mjs` there (writes the seven `.dc.html`).
2. In a Claude Code session with the `/design` skill: seed a fresh copy with the skill's helper, passing every `--artboard`, `--image logo-sm.png --image josh-desk.jpg --image josh-mat.jpg`, and `--canvas canvas.json`; run `--check`; republish to the artifact URL above with `contract: "0.1.31"` and the same favicon.
3. `tools/serve.mjs` and `tools/static-preview.mjs` were the local checking aids (static renders under `preview/`, served on `localhost:8765`); they are optional.

## Repo state at handoff

Uncommitted: `aidlc-docs/aidlc-state.md` (modified), `aidlc-docs/audit.md` (appended), `aidlc-docs/inception/` (new: eight reverse-engineering docs, requirements, landing-directions sources incl. two JPEGs), this file. Nothing under `src/` or `tests/` changed. `.claude/launch.json` was a temporary preview config and has been removed. Suggested first commit: `docs: reverse-engineering set, requirements and BJJ landing design sources (Unit 10 inception)`; the JPEGs are cleared to go in.
