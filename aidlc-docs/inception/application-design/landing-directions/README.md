# Landing-page directions (design canvas sources)

Application Design artifact for Unit 10 (BJJ-themed landing page), produced 2026-09-03 at v1.22.0. Direction chosen by the owner the same day; see `../../requirements/requirements.md` section 11.

- **Design canvas (Claude Design editor, saving enabled):** https://claude.ai/code/artifact/4e3d73fd-098f-472f-a3c9-0678be44349c
- **Design-system project (Claude Design):** "Portfolio Design System", https://claude.ai/design/p/0eace0af-92bf-4fa4-b035-9fa3dd8dab6d (14 preview cards, `styles.css` = `app.css` verbatim, tokens, self-hosted fonts, per-component usage notes)

## Canvas pages

| Page | Artboards |
|---|---|
| Lead | `Main.dc.html` (the chosen merge), `MainMobile.dc.html` (same at 390px), `Baseline.dc.html` (the site today) |
| Directions | `QuietBelt.dc.html`, `GamePlan.dc.html`, `LongRoad.dc.html`, `MatRoom.dc.html` (the four original explorations, kept for reference) |

## Files

| File | What it is |
|---|---|
| `Main.dc.html` | The lead: Game Plan hero (eyebrow, "Position before submission.", description, CTAs) with the desk/mat portrait switch to the right, the Guard > Pass > Mount > Submit chart under both, then the rank bar with degree stripes, About, Principles, The road (belt-ladder radios), Now; working theme toggle |
| `MainMobile.dc.html` | The same lead framed at 390px |
| `Baseline.dc.html` | The landing page as it ships today, for side-by-side comparison |
| `QuietBelt.dc.html` | The original quiet direction: unchanged hero plus rank bar, portrait switch and Principles |
| `GamePlan.dc.html` | The page as a positional flowchart with four colored sections |
| `LongRoad.dc.html` | Belt ladder beside career eras; radios highlight the matching rows |
| `MatRoom.dc.html` | Puzzle-mat ground, tape lines, scroll-laid rail |
| `canvas.json` | Pages, artboard layout, sticky notes (lead rationale and open questions, ranked interactive ideas, content needed, rejected ideas, one note per original direction), launch view |
| `parts/` | Sources the artboards are built from: `shared.css` mirrors `src/Portfolio.Web/wwwroot/app.css`; `header.html`, `footer.html`; one body per artboard; `Main.css`, `GamePlan.css`, `LongRoad.css`, `MatRoom.css`, `Lead.css`; `Main.js` (theme, degrees, belt red, stripes) and `Theme.js` |
| `build.mjs` | Assembles the seven `.dc.html` files from `parts/` |
| `logo-sm.png` | The header mark downsampled for embedding |

Tweaks declared on the artboards: `theme` (dark/light) on every artboard; `degrees` (0-6, starts at 0 so no stripe count is asserted) on the lead, Quiet Belt and Long Road; `beltRed` (brand red `#a63d40` or true belt red `#c8102e`) on the lead and Quiet Belt.

Proposed fixed constants used by the belt graphics (outside the admin token set): `--belt-black` #0c0c0c, `--belt-white` #e8e4dd, `--rank-white` #e6dfd0, `--rank-blue` #2b4c8c, `--rank-purple` #5a3d8a, `--rank-brown` #6b4423, `--rank-black` #0c0c0c.

## Re-seeding the canvas after edits

Edit under `parts/`, run `node build.mjs`, then seed a fresh copy with the Claude Code design skill's helper and republish to the same artifact URL. Never hand-edit the seeded output.

## Content status

Supplied by the owner on 2026-09-03 and placed on the lead, Quiet Belt, Long Road and Mat Room artboards: black belt with no stripes (the Degrees tweak stays at 0), promoted December 9, 2025 by Rodney Souza at Championship Mixed Martial Arts in Daphne, AL; belt dates and academies (white December 1, 2005 at Tamaso's MMA, Fairhope; blue January 30, 2018 and purple August 23, 2019 at Bagram BJJ, Bagram, Afghanistan; brown September 23, 2020 at Iron Grip BJJ, Fairhope; black December 9, 2025); full-stack .NET since January 2021, on the mat since December 2005; Tuesday and Thursday morning adult no-gi classes; the two portraits (`josh-desk.jpg`, `josh-mat.jpg`, downsampled to 720px) with the owner's alt text. The belt ladder on The road runs white to black, left to right, per the owner; on desktop hovering a belt glows it together with its table row and hovering a row glows its belt back (decoration only; every row is readable at rest); under 720px the ladder is hidden, each stacked row carries its own belt band above it, and the row passing through the middle of the screen glows via a CSS scroll timeline (static where unsupported); the colored belts carry a black bar with the stripes he earned before each promotion (2 on white, 3 on blue, 1 on purple, 4 on brown) and the black belt carries the red bar with none.

Drafted in the owner's voice from his own GitHub repo descriptions, pending approval: the CalCrony line (Building) and the home-lab line. Still open: `[CURRENT ROLE]` on the black-belt row and the dev-side column of The road (it reads "Before code" for 2005 to 2020 because coding started in January 2021); the tagline, the four chart node readings and the three principle readings remain drafts. The academy's name is "Championship Mixed Martial Arts" (confirmed by the owner on 2026-09-04).
