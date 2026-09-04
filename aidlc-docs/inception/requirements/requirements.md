# Requirements: Portfolio Review and BJJ-Themed Landing Page Directions

**Stage**: Inception, Requirements Analysis (minimal depth: the request is clear; ambiguity resolved with three questions).
**Date**: 2026-09-03. **Baseline**: `master` @ `e1825d9`, v1.22.0.

## 1. The ask (verbatim)

> review and document what is here. Find the pros and cons and gaps. I am thinking that this is the base theme. I would like to make a custom design based around the concept that I am heavily into BJJ (I am a black belt afterall). I would like to explore some creative touches to the landing page. What could I do that is bjj themed and possibly interactive?

Follow-up during execution: "make sure the design system syncs to claude design".

## 2. Intent analysis

| Intent | Reading | Deliverable |
|---|---|---|
| "review and document what is here" | Reverse-engineer the brownfield repo and record it in the repo | The eight artifacts under `inception/reverse-engineering/` |
| "pros and cons and gaps" | A candid quality assessment from engineering, UX and product angles | `reverse-engineering/code-quality-assessment.md` |
| "this is the base theme" | Keep the existing tokens, fonts, voice and layout; extend, do not replace | Every design direction reproduces the app pixel-for-pixel first |
| "custom design ... around BJJ ... creative touches ... possibly interactive" | Explore several genuinely different landing-page directions with working interactions | The design canvas (`inception/application-design/landing-directions/`) with four directions, a baseline and a mobile check |
| "design system syncs to claude design" | The site's tokens and components available as a Claude Design design-system project | A hand-authored design-system bundle synced through the Design Sync tool |

## 3. Decisions taken with the owner

| Question | Answer (verbatim) | Effect |
|---|---|---|
| Static mockups or working prototype? | "All four working" | Every direction carries its interaction inside the canvas; each artboard is `is_interactive` |
| How much review documentation? | "Full eight-file set" | All eight reverse-engineering files plus this requirements record |
| Which BJJ facts may go on the site? | "Degrees on your black belt, Academy name / where you teach, Promotion years / years training, A photo from the mat or in a gi" | Every direction is viable; values remain bracketed placeholders (`[DEGREE]`, `[ACADEMY]`, `[PROMOTION_YEAR]`, `[START_YEAR_BJJ]`, `[START_YEAR_DEV]`, `[BELT_YEARS]`, `[CLASSES]`, `[PHOTO_MAT]`) until supplied |

## 4. Functional requirements (design stage)

| Id | Requirement | Source |
|---|---|---|
| FR-1 | Document the current system in the eight reverse-engineering artifacts with `file:line` evidence | Ask; global AI-DLC workflow |
| FR-2 | Rank pros, cons and gaps for a senior .NET developer's portfolio, including a content checklist that needs no design decision | Ask |
| FR-3 | Reproduce the current landing page as a baseline artboard using the exact tokens, type ramp, spacing and copy from `wwwroot/app.css`, `Components/LandingSections.razor` and `Components/Layout/MainLayout.razor` | "base theme" |
| FR-4 | Present four landing-page directions on distinct axes (signature re-reading, structure, time, material), each with a stated motivation, an honest tradeoff, and one working interaction | Ask; design craft rule "settle the aesthetic with the user, not for them" |
| FR-5 | Include a 390px-wide artboard of the leading direction | Landing-page responsive rule |
| FR-6 | Provide a ranked list of BJJ-themed interactive ideas with effort, degradation behaviour and accessibility notes, plus a rejected list with reasons | "What could I do that is bjj themed and possibly interactive?" |
| FR-7 | Keep every owner fact as a visibly bracketed placeholder; never invent degrees, years, academy names or photos | Content rule |
| FR-8 | Publish the site's design system (tokens, type, buttons, nav, cards, chips, forms, footer, brand marks) as a Claude Design design-system project with preview cards and usage notes | Follow-up ask |

## 5. Non-functional requirements and constraints for any later implementation

| Id | Constraint | Evidence |
|---|---|---|
| NFR-1 | Public pages stay static SSR; no `InteractiveServer` on `/`; interactivity is CSS or small first-party JS in the `site.js` pattern | `Program.cs`, `wwwroot/js/site.js`, privacy claims |
| NFR-2 | No CDN, no third-party scripts, no external fonts; inline SVG icons only, no emoji | `app.css:4-26`, `Components/Icon.razor`, `README.md` |
| NFR-3 | `LandingSections` must render correctly inside the inert admin preview: nothing `position: fixed`, resting state complete | `Components/Admin/ThemeEditor.razor:118-120` |
| NFR-4 | Self-hoster promise: owner-specific UI is data-driven or behind an env switch (recommended `SITE_FLAVOR=bjj`), never hardcoded into shared markup | `README.md:3-5`, `LandingSections.razor:36` |
| NFR-5 | Belt and rank colours are fixed constants, not admin-overridable tokens; the 26-token catalog and its pinned tests stay untouched | `Services/ThemeRules.cs`, `tests/Portfolio.Tests/ThemeRulesTests.cs` |
| NFR-6 | Every new animation is registered in the reduced-motion block; scroll-driven animation is gated by `@supports (animation-timeline: scroll())` with a finished static fallback | `app.css:282-285` |
| NFR-7 | Accessibility: WCAG 2.1 AA; keyboard operability for every interactive device; radio or `details` patterns over hover-only reveals; belt red never carries text | `app.css:51`, contrast rules |
| NFR-8 | No CLS from new hero elements (fixed dimensions or aspect ratios); LCP element untouched | performance basics in the assessment |
| NFR-9 | One PR per phase sized for the Copilot review gate; conventional-commit titles from the CI allow-list | `CONTRIBUTING.md`, `.github/workflows/ci.yml` |

## 6. Direction candidates (summary; the canvas is the record)

| Direction | Axis | Device | Working interaction | Main tradeoff |
|---|---|---|---|---|
| Quiet Belt (lead) | Re-read the marks already there: dark = black belt, light = white gi | Rank bar with degree stripes on the hero seam; Principles section; desk/mat portrait switch | Theme toggle as belt/gi reveal; portrait and principles radios; stripe stagger on load | Quiet; the tagline carries the new information |
| Game Plan | Structure: the page is a positional flowchart | Inline SVG chart Guard → Sweep → Pass → Mount → Submit; the four brand colours become the four nodes | Hover/focus trace on edges; nodes anchor to sections | Metaphor must hold in every section; largest build |
| Long Road | Time: durability shown with dates | Two-ladder timeline, belts beside career eras on a shared year axis | Belt bands as radio stops highlighting matching rows | Needs the most facts; dates turn a portfolio toward a CV |
| Mat Room | Material: the place, not the words | Puzzle-mat ground at 3% contrast, mat-tape lines framing the hero, ribbons squared into tape ends | Outer tape line extends on scroll | Highest taste risk; texture fights admin-chosen backgrounds |

## 7. Interactive ideas, ranked

1. Rank bar with degree stripes on the hero seam (S, CSS).
2. Theme toggle as black belt / white gi (`title` swap; the accessible name stays functional) (S).
3. Desk/mat portrait switch in the empty photo slot (M; needs a second photo and an `OwnerPhotoService` slot).
4. Principles pairs with an optional "On the mat / In the code" radio (S static, M toggle).
5. Game-plan chart with hover/focus trace (M to L, CSS).
6. Belt-ladder timeline with radio stops (M; needs an eras field, recommended as a `SiteContent` column).
7. Belt-degree scroll progress in `Home.razor` via `animation-timeline: scroll()` behind `@supports` (S; opinion split: progress bars are common, kept optional).
8. Scroll-laid mat tape lines (M, CSS, `@supports`).
9. Tatami texture via `repeating-linear-gradient` on `color-mix(var(--text) 5%, transparent)` (S).
10. "oss" easter egg in `site.js` (S; optional, arguably self-indulgent).
11. Microcopy: contact success "I read everything and reply when I'm off the mat", 404 "Out of bounds", footer motto `POSITION BEFORE SUBMISSION` or an "Oss." sign-off, reconnect "hold position" (S).

Rejected: "Tap to submit" (confusing on mobile, the first pun everyone makes); "Oss" in the hero; belt-coloured skill chips; choke or armbar icons; cursor and parallax tricks; gi/no-gi as theme labels; theming the projects carousel; italic Fraunces (no italic face bundled); a live "last class taught" line.

## 8. Content required from the owner

`[DEGREE]` (0-6), `[PROMOTION_YEAR]`, optionally `[PROMOTED_BY]`, `[ACADEMY]` and `[CITY]`, `[START_YEAR_BJJ]`, `[START_YEAR_DEV]`, `[BELT_YEARS]` (white through black), `[CLASSES]` taught, the three principle readings in his own words, `[SENIORITY_TITLE]` confirmation, `[PHOTO_DESK]` and `[PHOTO_MAT]` with alt text (4:5-croppable, up to 5 MB), optionally `[HOME_LAB_FACTS]`.

## 9. Decisions to lock before Construction (Unit 10)

Locked by the owner on 2026-09-04; the authoritative list is in `construction/plans/unit10-bjj-landing-handoff.md`: flavor switch yes; copy in `SiteContent` columns; second portrait slot yes; chart nodes link to `#principles`; hero h1 "Position before submission."; JPEGs may be committed; academy name "Championship Mixed Martial Arts".

| Decision | Recommendation |
|---|---|
| Opt-in switch vs hardcoding BJJ theming | `SITE_FLAVOR=bjj` parsed in `Services/SiteConfig.cs`; unknown values fall back to default |
| Belt colours | Fixed `:root` constants; record as `docs/adr/0002-belt-colors-are-fixed-constants.md` |
| Interactivity on `/` | CSS and tiny JS only; no SignalR circuit |
| "Mat" navigation | In-page anchor `/#mat` first; a `/about` page only if the material outgrows the landing page |
| Timeline eras storage | `SiteContent.Eras` text array edited at `/admin/site`, env fallback `SITE_TIMELINE` |
| Safety net before visual changes | `LandingSectionsRenderTests` with `HtmlRenderer` (no new package); delete `UnitTest1.cs` |

## 10. Out of scope for this stage

Any change under `src/` or `tests/`; production `.env` edits (listed as the Phase 0 checklist in the assessment); a dedicated `/about` page; project detail pages; testimonials.

## 11. Direction chosen (2026-09-03)

Owner feedback on the canvas, verbatim: "I like the Quiet Belt having the picture, but I like the Game Plan layout better for the top section. If we could do that with the picture to the right of the H1 and its p description (still above the Guard to Submit) it would be best. The Long Road's "The Road" and "Now" would be nice to have on the Quiet Belt (lead) below Principles section."

The lead artboard (`Main.dc.html`) is now that merge, top to bottom: header; hero with the Game Plan eyebrow, "Position before submission.", the game-plan description and the CTA row on the left and the desk/mat portrait on the right; the Guard, Pass, Mount, Submit chart spanning both columns; the rank bar; About; Principles; The road; Now; footer. The four Game Plan content sections were not carried over, so every chart node currently scrolls to Principles. The original Quiet Belt, Game Plan, Long Road and Mat Room artboards stay on the canvas's second page for reference. The Degrees tweak starts at 0 because the owner's black belt has no stripes yet.

Open for the owner: keep the nodes pointing at Principles or restore short Position / Transition / Control / Finish sections; rewrite the tagline, the four node readings and the three principle readings in his own words.
