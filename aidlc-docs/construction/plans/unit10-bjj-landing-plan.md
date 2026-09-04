# Unit 10 Plan — BJJ-themed landing page

**Written**: 2026-09-04 (Code Generation, Part 1). **Baseline**: `master` @ `c29e6bc` (v1.22.0 plus the inception docs). **Status**: approved by the owner 2026-09-04 together with the functional design; Part 2 (execution) started the same day with Phase 1.

## Execution model (owner instruction, 2026-09-04)

- The main session orchestrates. Each phase is executed by a fresh Sonnet subagent with its own context, briefed with this plan, the functional design and the constraints; it works on the phase branch, gets `dotnet build -warnaserror` and `dotnet test` green, ticks its plan boxes and commits, but does not push.
- When the phase agent reports done, five review subagents run in parallel, one per area: correctness and bugs (production-breaking issues, logic bugs and edge cases before formatting), security (hardcoded secrets, injection, OWASP Top 10), framework awareness (Blazor, EF Core, .NET 10 and CSS patterns), maintainability (readability, modularity, repository standards), performance.
- Findings are remediated on the branch, then the PR is opened and the Copilot review gate and squash-merge follow per `CONTRIBUTING.md`. Plan boxes, `aidlc-state.md` and `audit.md` are updated at each step.

Inputs: `construction/plans/unit10-bjj-landing-handoff.md` (the brief, seven locked decisions), `construction/unit10-bjj-landing/functional-design/domain-entities.md` and `business-rules.md` (BR-1 to BR-19), the exact markup and CSS in `inception/application-design/landing-directions/parts/` (`Main.body.html`, `shared.css`, `Main.css`, `GamePlan.css`, `LongRoad.css`, `Lead.css`), the approved canvas https://claude.ai/code/artifact/4e3d73fd-098f-472f-a3c9-0678be44349c and the design-system project https://claude.ai/design/p/0eace0af-92bf-4fa4-b035-9fa3dd8dab6d.

## Non-negotiables (checked on every phase)

- Public pages stay static SSR; no `InteractiveServer` on `/`; no inline `onclick` additions.
- `LandingSections` renders correctly inside the inert admin theme preview: nothing `position: fixed`, complete at rest.
- Belt colors are fixed `:root` constants (ADR 0002); the theme token catalog and its tests are untouched.
- Every new animation and transition joins the one reduced-motion block; scroll-driven effects are `@supports`-gated with a finished static fallback.
- No CDN, no third-party scripts, no emoji; icons are inline SVG.
- CSS values are copied from `landing-directions/parts/`, not from memory; deviations are listed below and in the PR.
- Every owner fact exactly as recorded in the handoff (content sheet at the end); none of it in code, tests or `.env.example`.
- One PR per phase, conventional-commit title from the CI allow-list, `dotnet build -warnaserror` and `dotnet test` green locally before pushing, Copilot review gate per `CONTRIBUTING.md` until a pass produces no new actionable comments, then squash-merge.

## Design refinements made in Functional Design (say so if you disagree)

1. Structured copy is stored as `text[]` lines with `|` between fields (`GamePlan`, `Principles`, `Eras`, `Now`) and edited in textareas, the way `Skills` works today. Env fallbacks use literal `\n` between lines like `SITE_ABOUT`.
2. Env names: `SITE_FLAVOR`, `SITE_HERO_EYEBROW`, `SITE_GAME_PLAN`, `SITE_BELT_CAPTION`, `SITE_BELT_DEGREES`, `SITE_PRINCIPLES`, `SITE_ERAS` (section 9 of requirements.md said `SITE_TIMELINE`), `SITE_NOW`, `OWNER_PHOTO_FLIP_FILE`, `OWNER_PHOTO_FLIP_ALT`.
3. Counts: the game plan is exactly four nodes or hidden; principles 1 to 6; now tiles 1 to 8; eras 1 to 12. Belt names are the closed set of five. Degrees 0 to 6 (the stripe bar's width).
4. `BeltDegrees` stays its own column (decision 2), and the editor refuses a save where a black-belt era's stripes disagree with it (BR-9).
5. Mobile scroll-glow timelines are named by row index (`era-1`..`era-8`) instead of by belt, so two eras on the same belt cannot share a timeline; rows past eight are static. The keyframes, ranges and glow values are copied verbatim.
6. The road table's band column drops `aria-hidden` from the `th`/`td` (the band span inside stays hidden from assistive tech) so screen readers see a consistent column count.
7. The portrait switch moves from Phase 2 to Phase 4, where the second slot it needs is built; Phase 2 keeps the single photo exactly as today.
8. The rank bar is always drawn as a black belt in v1 (BR-6); a "current belt" field is a recorded follow-up, not part of this unit.

## Phases and PRs

| Phase | Branch | PR title (conventional) | Size |
|---|---|---|---|
| 0 | none | owner actions, no code | |
| 1 | `test/landing-render-tests` | `test: render tests for the landing sections (HtmlRenderer safety net)` | small |
| 2 | `feat/bjj-landing-foundation` | `feat: BJJ landing flavor (hero game plan, rank bar, principles, admin copy fields)` | large (~18 files) |
| 3 | `feat/bjj-road-and-now` | `feat: the road and now sections for the BJJ landing` | medium |
| 4 | `feat/owner-photo-flip` | `feat: second owner photo slot with the hero portrait switch` | medium |
| 5 | `perf/landing-closeout` | `perf: display-font preload and delegated nav handlers, BJJ landing docs` | small |

Each phase ends with the same gate: build, test, PR, Copilot rounds, squash-merge, then this plan's checkboxes, `aidlc-state.md` and `audit.md` are updated in the same interaction.

---

## Phase 0 — owner actions (no code)

- [x] The seeded post "A quick markdown tour" stays published for now (owner decision 2026-09-04; nothing in Unit 10 depends on it; options recorded in audit.md).
- [ ] Optional: align the local `.env` with production (`PUBLIC_BASE_URL=https://joshuamykitta.dev`, the live tagline and skills, `SEED_DEMO_DATA=false`). I will not edit `.env` unless you ask.
- [x] The remaining drafts (tagline, four node readings, three principle readings) ship as drafts with the plan approval of 2026-09-04; editable at `/admin/site` any time after Phase 2.

## Phase 1 — `test:` render safety net

Purpose: pin today's landing markup before any visual change (BR-12, BR-13).

- [x] `tests/Portfolio.Tests/LandingSectionsRenderTests.cs` (new): a helper that builds a `ServiceCollection` (logging, `SiteConfig`, `OwnerPhotoService`), an `HtmlRenderer`, and renders `LandingSections` with an `EffectiveSiteContent` on the renderer's dispatcher. No new package: the test project already reaches the ASP.NET Core shared framework through the project reference.
- [x] Tests: h1 carries `HeroHeading`; tagline present when set and absent when blank; About renders one `<p>` per line and the section is absent with no About and no skills; one chip per skill; GitHub and LinkedIn buttons only when configured; `has-photo` plus `<img class="owner-photo">` with the normalized alt when the photo file exists (temp file), neither when unconfigured; the rendered HTML contains no `position: fixed` and no `position:fixed`.
- [x] `tests/Portfolio.Tests/Portfolio.Tests.csproj`: link `src/Portfolio.Web/wwwroot/app.css` as content copied to the output so tests can scan it; test that no selector declaring `position: fixed` in `app.css` names a class present in the rendered landing HTML (today only `#blazor-error-ui` is fixed).
- [x] Delete `tests/Portfolio.Tests/UnitTest1.cs` (empty scaffold).
- [x] `construction/build-and-test/unit-test-instructions.md`: correct the stale counts and fixture table (real numbers taken from `dotnet test` output after this phase).
- [x] Gate: build `-warnaserror`, test, PR, Copilot rounds, squash-merge; update plan, state, audit. Done 2026-09-04: five-area internal review, remediation commit, PR #89, Copilot round 1 (3 comments, all fixed), round 2 approval recommended, squash-merged as `c1380a2` (445 tests).

## Phase 2 — `feat:` foundation (flavor, hero game plan, rank bar, principles, admin fields)

Data (domain-entities.md sections 1-3; BR-1 to BR-7, BR-11):

- [x] `Services/SiteFlavor.cs` (new): `SiteFlavor { Default, Bjj }` and `Parse(string?)`. Deviation: `Parse` lives on a companion `SiteFlavorRules` static class, not on the enum itself — C# enums can't host static members; follows this repo's Foo/FooRules split (SiteContentRules, ThemeRules).
- [x] `Services/SiteConfig.cs`: `Flavor`, `HeroEyebrow`, `GamePlanLines`, `BeltCaption`, `BeltDegrees`, `PrincipleLines` from the env names above (`\n` splitting shared with `SITE_ABOUT`); all optional, blank = null/empty.
- [x] `Services/BjjRules.cs` (new, pure): `GamePlanNode`, `Principle` records; `SplitLines`, `SplitFields`; `ParseGamePlan`, `ParsePrinciples` (lenient); `ValidateGamePlan`, `ValidatePrinciples`, `ValidateDegrees` returning the first friendly error or null; `MaxDegrees = 6`. Deviation: the `Belt` enum is deferred to Phase 3 (only `Era` needs it; adding it now would be untested dead code) — the code-generation briefing for this phase explicitly scoped BjjRules to game plan/principles/degrees and told Eras/Now to stay out.
- [x] `Data/SiteContent.cs`: `HeroEyebrow`, `GamePlan` (`List<string>?`), `BeltCaption`, `BeltDegrees` (`int?`), `Principles` (`List<string>?`).
- [x] `Services/SiteContentRules.cs`: new limits; `EffectiveSiteContent` gains `HeroEyebrow`, `GamePlan`, `BeltCaption`, `BeltDegrees`, `Principles`; `Resolve` parses with `BjjRules`; new `SiteContentDraft` record (every raw form string) and `Validate(draft)` that runs `CheckLengths` plus the format checks; `LinesText` round-trip helper (the `SkillsText` pattern).
- [x] `Services/SiteContentService.cs`: `SaveAsync(SiteContentDraft)` replaces the positional signature; upsert writes the new columns.
- [x] `Data/AppDbContext.cs`: max lengths for the two new text columns; migration `AddBjjLandingCopy` via `dotnet ef migrations add` (five columns: `HeroEyebrow`, `GamePlan`, `BeltCaption`, `BeltDegrees`, `Principles`).
- [x] `Components/Admin/SiteContentEditor.razor`: under `Bjj` only, a "Mat" group: Hero eyebrow (input), Game plan (textarea, `term | reading | how`, four lines), Belt caption (input), Belt degrees (number 0-6), Principles (textarea, `maxim | reading`); placeholders show the env values; the save path uses `Validate(draft)`.
- [x] `.env.example`: `SITE_FLAVOR` and the five new keys, documented with commented generic examples (Jane Developer / generic BJJ terms), blank by default.

UI (markup from `Main.body.html`; CSS from `shared.css` (`.visually-hidden`, belt constants), `GamePlan.css`, `Main.css`, `Lead.css`):

- [x] `wwwroot/app.css`: belt and rank constants in `:root` with an ADR 0002 comment; `.visually-hidden` in Base; a new banner section "Landing (BJJ flavor)" after Sections holding the hero eyebrow margin, `.game-plan`/`.gp-node` rules (desktop and 720px), `.rank-bar`/`.belt-body`/`.belt-bar`/`.belt-tip` rules and the `stripe-on` keyframes with the six delays, `.principles` rules; `.belt-bar i` added to the existing reduced-motion block and `.gp-node a` transitions disabled there. Deviation: `.gp-node::after`/`.gp-node::before` (arrow-connector color transitions) are also disabled in the reduced-motion block, beyond the single `.gp-node a` the plan named — BR-14 says *every* new transition joins that block, and both pseudo-elements declare their own `transition:`; the design source's own reduced-motion snippet only lists `.gp-node a`, so this is a deliberate widening, not a value change. Also omitted GamePlan.css's `.eyebrow-red/-gold/-green/-blue` and `.section .placeholder` rules — they style a per-node landing-section variant explicitly out of scope for v1 (decision 4) and nothing in this phase's markup ever emits those classes.
- [x] `Components/GamePlan.razor` (new): `<ol class="game-plan hero-plan" aria-label="Game plan">`, one `<li class="gp-node gp-{color}">` per node, positional colors, `<a href="#principles">` with `.term`, `.read`, `.how` spans (`.how` omitted when blank).
- [x] `Components/RankBar.razor` (new): `<figure class="rank-bar">` with the belt body, the red bar carrying `BeltDegrees` `<i>` stripes, the tip, and the `figcaption`.
- [x] `Components/LandingSections.razor`: under `Bjj`, the eyebrow before the h1, the chart after the actions inside the hero container (`grid-column: 1 / -1` when a photo makes it a grid), the rank bar in its own `.container` between hero and About, and the Principles section with `id="principles"` after About; each blank-hides; the `Default` branch is unchanged markup (pinned by `Render_DefaultFlavor_IdenticalWhetherOrNotBjjDataIsPresent`).
- [x] `docs/adr/0002-belt-colors-are-fixed-constants.md` in the ADR 0001 format (context, considered options: theme tokens, derived from brand colors; consequences).

Tests:

- [x] `SiteConfigTests`: flavor parsing (`bjj`, `BJJ `, blank, `foo`), new env values split and trimmed, degrees parsing, defaults null/empty.
- [x] `BjjRulesTests` (new): game-plan parsing and count rule, principles parsing, degrees range, lenient drops vs strict messages naming the line number.
- [x] `SiteContentRulesTests`: `Resolve` precedence for the new fields (override, env, hidden); `Validate` messages; `LinesText`/`ParseLines` round trip.
- [x] `LandingSectionsRenderTests`: `Default` flavor output unchanged with BJJ data present; `Bjj` renders eyebrow, h1, four `#principles` links with positional classes, the rank bar with N stripes, Principles with `id="principles"`; each section absent when its data is empty; no fixed positioning; HTML metacharacters encoded.
- [x] `AppCssTests` (new): the seven constants exist on `:root` with the ADR values (and are absent from the light-theme block); every selector declaring `animation` or `transition` inside the landing banner section appears in the reduced-motion block; the section contains no `position: fixed`; exactly one `prefers-reduced-motion` at-rule in the file. `LandingRenderHarness.MaximalConfig`/`MaximalContent` extended to Bjj flavor with every BJJ field populated so the fixed-position cross-check covers the new markup.
- [x] Local visual check: done, partially. Ran the app locally (`dotnet run` against an ephemeral, isolated Postgres container — not the shared `portfolio-dev` compose stack, to avoid disturbing it) with `SITE_FLAVOR=bjj` and generic sample `SITE_*` values exported for the run only (nothing written to `.env`). Verified in the Browser pane: 1440px dark (hero, eyebrow, 4-node game-plan chart with correct positional colors and arrows, rank bar with 3 stripes, About, Principles all correct), 390px mobile dark (hamburger nav, chart stacks to 1 column with down-arrow connectors, rank bar restyles, Principles stacks to 1 column — all correct), and 1440px light (belt stays black, rank stripe bar stays the brand red, unaffected by the light palette — confirms ADR 0002). Not verified: reduced motion (the Browser tool has no `prefers-reduced-motion` emulation control; the CSS itself is pinned by `AppCssTests` instead) and the `/admin/theme` preview frame (needs a real admin OAuth sign-in with no credentials available in this environment; confirmed instead that `/admin/theme` correctly redirects an unauthenticated visitor to `/signin`).
- [x] Gate: build `-warnaserror`, test, PR, Copilot rounds, squash-merge; update plan, state, audit. Done 2026-09-04: five-area internal review, remediation commits, PR #90, Copilot rounds 1 to 3 (three robustness fixes, one tail-field parsing fix, then no new comments), squash-merged as `e6a4a75` (567 tests).
- [ ] Owner content entry after deploy: set `SITE_FLAVOR=bjj` in the production `.env`, then paste the Phase 2 rows of the content sheet at `/admin/site` (hero heading, eyebrow, tagline, game plan, belt caption, degrees, principles).

## Phase 3 — `feat:` The road and Now

Data (BR-8 to BR-11):

- [x] `SiteConfig`: `EraLines`, `NowLines` from `SITE_ERAS`, `SITE_NOW`.
- [x] `BjjRules`: `Era`, `NowItem` records; `ParseEras` (date `YYYY-MM-DD`, belt closed set, stripes 0-6 blank = 0), `ParseNow`; `Rungs(eras)` (one per distinct belt, first-appearance order, stripes of the last era of that belt); `ValidateEras`, `ValidateNow`, `ValidateDegreesAgainstEras` (BR-9); `CssName(Belt)`.
- [x] `SiteContent`: `Eras`, `Now` (`List<string>?`); `SiteContentRules` limits, `Resolve`, `Validate`; migration `AddRoadAndNow`.
- [x] `SiteContentEditor`: Eras textarea (`date | belt | stripes | gym | location | role`) and Now textarea (`label | value`) in the Mat group.
- [x] `.env.example`: `SITE_ERAS`, `SITE_NOW` with commented generic examples.

UI (markup from `Main.body.html`; CSS from `LongRoad.css`):

- [x] `Components/BeltBand.razor` (new): `<span class="belt-band {belt}" aria-hidden="true"><span class="bar">{stripes}</span></span>`; the black belt's bar is red per the CSS.
- [x] `Components/Road.razor` (new): the `.road` wrapper with the `aria-hidden` ladder (`.rung` per rung, `.name` labels) and the `.road-table` (`th` Date, Belt, Gym, Location, Role plus the plain band column), one `<tr class="row era-{n}" data-belt="{belt}">` per era with the band cell, `<time datetime>` date, swatch plus belt name, gym, location, role, `data-label` on every cell for the stacked layout.
- [x] `LandingSections.razor`: "The road" section (eyebrow, h2 "Two ladders, one clock") and the "Now" section (`<dl class="now">`), both under `Bjj`, both blank-hide.
- [x] `app.css`: ladder, `.belt-band` and `.bar`, `.road-table`, the swatch, the hover-link rules (band glows row, row glows band, the two-declaration `box-shadow` and `background` fallbacks kept), the 720px stacking rules, the `@supports (animation-timeline: view())` block rewritten with index-based names (`timeline-scope: --era-1 .. --era-8`), `row-glow` and `band-glow` keyframes, `.now` tiles; `.row`, `.belt-band` and `.swatch` transitions plus the scroll animations added to the reduced-motion block.

Tests:

- [x] `BjjRulesTests`: eras parsing (good line, bad date, bad belt, blank stripes, too many stripes), rungs derivation with a repeated belt, now parsing, degrees-vs-eras check.
- [x] `SiteContentRulesTests`: resolve and validate for eras and now.
- [x] `LandingSectionsRenderTests`: five rows with `data-belt`, `era-1`..`era-5`, five rungs in ladder order, band cell per row, h2 text, four now tiles; sections absent when empty; no fixed positioning.
- [x] `AppCssTests`: the `@supports` gate wraps every `animation-timeline` use; the reduced-motion invariant still holds for the new selectors.
- [x] Local visual check as in Phase 2, plus Firefox-style fallback (disable scroll timelines) and a hover pass on the ladder and rows. Done 2026-09-04: ran the app locally (`dotnet run`, Development environment, against an ephemeral, isolated Postgres container on a throwaway host port — not the shared `portfolio-web`/`portfolio-db` stack) with `SITE_FLAVOR=bjj` and generic sample `SITE_*` values exported for the run only (nothing written to `.env`). Verified in the Browser pane: 1440px dark (road ladder with correct belt colors and stripe counts, 5-row table with correct data in entered order, Now's 4-tile grid), 1440px light (belt colors stay fixed per ADR 0002 — black belt stays black, rank colors unaffected by the light palette), 390px mobile dark (ladder hidden, rows stack with per-row belt bands and `data-label`s, scroll-driven glow visibly lighting the row nearest the viewport center as the page scrolls), and a hover pass on desktop confirming both directions of the `:has()` glow (hovering a ladder band tints and rings its table row; hovering a row rings its ladder band back). Not verified: the Firefox-style static fallback for browsers without `animation-timeline: view()` support (this environment's Browser pane is Chromium-based, which supports it) — the `@supports` gate is instead pinned by `AppCssTests` (every `animation-timeline` declaration has an `@supports (animation-timeline: view())` ancestor), and the fallback is a plain, unconditional static row with no dependency on the gate passing, so its correctness follows from the base (ungated) CSS being correct, which the render tests and this same visual check already cover.
- [x] Gate: build `-warnaserror` and `dotnet test` green (0 warnings, 666 tests); five-area internal review and remediation (BR-18 fixture sweep, effective-value BR-9 check, reduced-motion overrides made to win the cascade with `!important`, one 720px block per section, Rungs resolved once); Copilot round 1 with zero comments; PR #92 squash-merged 2026-09-04 as `7dddfdf`; plan, state and audit updated.
- [ ] Owner content entry after deploy: paste the eras and now rows of the content sheet at `/admin/site`.

## Phase 4 — `feat:` second portrait slot and the hero switch

- [x] `SiteConfig`: `OwnerPhotoFlipFile`, `OwnerPhotoFlipAlt`.
- [x] `Services/OwnerPhotoService.cs`: `OwnerPhotoSlot { Primary, Flip }`; every member takes a slot defaulting to `Primary` (callers and existing tests untouched); path resolution per slot.
- [x] `Endpoints/SeoEndpoints.cs`: `/owner-photo-flip` sharing one handler with `/owner-photo`.
- [x] `SiteContent.OwnerPhotoFlipAlt` (200); rules, resolve ("Portrait of {owner}" fallback), validate; migration `AddOwnerPhotoFlipAlt`.
- [x] `SiteContentEditor`: second upload control, remove button, preview and alt field for the mat portrait, shown only when `OWNER_PHOTO_FLIP_FILE` is set.
- [x] `LandingSections.razor`: when both slots resolve, the `.photo-slot` with `.photo-stack` (two `.photo-tile` images, desk first) and the `pill-switch photo-switch` fieldset (visually hidden legend "Show photo", radios `ph-desk` checked and `ph-mat`, labels "At the desk" and "On the mat"); otherwise today's single `<img>`. `fetchpriority="high"` on the primary hero image in both cases. Deviation: the desk `<img>` inside the switch does not also carry the `owner-photo` class the single-image path uses — `.owner-photo`'s own `margin-top: 3rem` rule, additive with `.photo-stack`'s own (correct) `margin-top: 3rem`, would double-offset the image inside the absolutely-positioned `.photo-tile` and clip its bottom edge under `overflow: hidden`. The design source's own markup (`Main.body.html`) has no class on either tile `<img>` either, so this keeps the rendered tile faithful to the source; only `fetchpriority`/`loading` were added per this plan row.
- [x] `app.css`: `.pill-switch` rules from `shared.css`, `.photo-slot`/`.photo-stack`/`.photo-tile` and the `:has(#ph-mat:checked)` crossfade from `Main.css`, the 720px placement rules; `.photo-tile` transition added to the reduced-motion block.
- [x] `.env.example`: `OWNER_PHOTO_FLIP_FILE`, `OWNER_PHOTO_FLIP_ALT`.
- [x] Tests: `OwnerPhotoServiceTests` slot cases (unconfigured flip, url per slot, save and delete per slot, primary untouched by a flip save); `SiteConfigTests`; `LandingSectionsRenderTests` (switch only when both files exist, desk radio checked, both alts, single image otherwise, `fetchpriority` present); `AppCssTests`. Deviation: two pre-existing `AppCssTests` synthetic cases used `.owner-photo` as their "known to render" example class; since `MaximalConfig`/`MaximalContent` now renders the switch (both slots configured) and the switch's desk image deliberately omits that class (see above), those two cases were repointed to `.hero-text` (an unconditional class) — the mechanism each case tests (pseudo-class-ignored parsing, `@media`-nesting parsing) is unchanged.
- [x] Local visual check: done, partially. Ran the app locally (`dotnet run`, Development environment, against an ephemeral, isolated Postgres container on a throwaway host port — not the shared `portfolio-web`/`portfolio-db` stack) with `SITE_FLAVOR=bjj`, generic sample `SITE_*` values, and two generic placeholder PNGs (existing repo icons, not the owner's real photos) as `OWNER_PHOTO_FILE`/`OWNER_PHOTO_FLIP_FILE`, all exported for the run only (nothing written to `.env`). Verified in the Browser pane: the hero renders the photo-stack with both tiles and the `pill-switch photo-switch` fieldset, "At the desk" checked and highlighted by default; clicking "On the mat" crossfades to the mat photo and highlights that pill instead; keyboard operation confirmed via the DOM (natural Tab order reaches `#ph-desk` then `#ph-mat` right after the hero's action buttons, before the game-plan links; pressing the Left arrow while focus was on the mat radio moved both focus and `:checked` to the desk radio; `document.activeElement.matches(':focus-visible')` was `true` with the label's computed `outline` equal to the `.pill-switch input:focus-visible + label` rule's `2px solid var(--c-blue)`); confirmed via `getComputedStyle`/`getBoundingClientRect` that `.photo-slot` and `.photo-switch` are `position: relative`/`static` (never fixed) and genuinely scroll with the page (a scrolled screenshot that visually looked "stuck" turned out to be the tall hero row's normal geometry, not a positioning bug); dark and light theme both render the switch correctly with the same accent styling; 375px mobile emulation showed `.photo-slot` reordered above the hero text, centered, and width-capped, with the pill switch centered underneath, exactly per the 720px CSS block. Not verified: reduced motion (the Browser pane has no `prefers-reduced-motion` emulation control, as in Phases 2-3; the CSS itself is pinned by `AppCssTests` instead) and the live `/admin/site` editor or the `/admin/theme` preview frame (both need a real admin OAuth sign-in with no credentials available in this environment; confirmed instead that `/admin/theme` correctly redirects an unauthenticated visitor to `/signin`, matching Phases 2-3). The ephemeral container, temp photo files and browser tab were all cleaned up afterward; the shared `portfolio-web`/`portfolio-db` stack was confirmed undisturbed (unchanged uptime) throughout.
- [ ] Gate: build, test, PR, Copilot rounds, squash-merge; update plan, state, audit.
- [ ] Owner actions after deploy: set `OWNER_PHOTO_FLIP_FILE` (a path on the same read-write mount as the primary photo), upload the mat portrait and paste both alt texts at `/admin/site`.

## Phase 5 — `perf:` close-out

- [ ] `Components/App.razor`: `<link rel="preload" as="font" type="font/woff2" crossorigin>` for `fonts/fraunces-latin.woff2` using the same un-fingerprinted URL the CSS requests (a fingerprinted `@Assets` URL would download the font twice).
- [ ] `wwwroot/js/site.js`: one delegated click handler keyed on `data-action` (`toggle-nav`, `toggle-theme`, `scroll-projects` with `data-direction`); `MainLayout.razor` and `Projects.razor` lose their four inline `onclick` attributes; the global `__toggleNav` and `__scrollProjects` stay exported until nothing references them, then go.
- [ ] Optional, owner's call: under `Bjj`, the theme toggle's `title` reads "Switch to the white gi (light theme)" / "Switch to the black belt (dark theme)" (set by `theme.js` from a `data-flavor` attribute on `<html>`); the `aria-label` stays functional.
- [ ] `README.md`: a Features bullet for the opt-in BJJ landing flavor, the Configuration paragraph, env-table rows for every new key.
- [ ] `CONTEXT.md`: glossary entries Belt, Degree, Era, Flavor (and the road, the game plan) in the existing "avoid" format.
- [ ] `construction/build-and-test/unit-test-instructions.md`: final counts and the new fixtures.
- [ ] `aidlc-state.md` Unit 10 complete; `audit.md` close-out entry; memory notes updated.
- [ ] Gate: build, test, PR, Copilot rounds, squash-merge.

## Out of scope (recorded follow-ups)

- A "current belt" field so the rank bar can draw a non-black belt (BR-6).
- Per-node landing sections for Guard, Pass, Mount, Submit (decision 4 keeps `#principles` for v1).
- Kids' and coral belts in the closed belt set.
- Security headers, rate limiting and the other engineering cons from the reverse-engineering pass (separate units).

## Risks and how the plan handles them

- **Copilot review volume on Phase 2**: it is the largest PR (about eighteen files). If the first review round is noisy, split the follow-up commits by area (data, editor, UI) rather than the PR.
- **`:has()` and `color-mix` support**: both ship in every current evergreen browser; every rule using them is decoration on top of a readable resting state (BR-13), and the two-declaration fallbacks from the design CSS are kept.
- **Scroll-driven animations**: Chromium only today; Firefox and Safari get the static rows through the `@supports` gate (BR-14).
- **Migration drift**: three small migrations across three PRs, each generated with `dotnet ef migrations add` and applied on startup like every earlier one.

## Content sheet (the owner's values, exact; paste at `/admin/site`)

Phase 2 fields:

| Field | Value |
|---|---|
| Hero heading (h1) | `Position before submission.` |
| Hero eyebrow | `Joshua Mykitta · Software Engineer` |
| Tagline (draft) | `A game plan borrowed from the mat: secure the application first, build the tooling that improves position, then finish. Secure software engineer; Brazilian jiu-jitsu black belt.` |
| Belt caption | `Black belt · Championship Mixed Martial Arts, Daphne, AL · promoted December 9, 2025 by Rodney Souza` |
| Belt degrees | `0` |

Game plan (four lines, readings are drafts):

```text
Guard | Secure the position | Auth, secrets, boundaries. Get the position first.
Pass | Improve the position | Developer tooling that makes the next move easier.
Mount | Keep control | Tests, reviews, boring deploys.
Submit | Finish | Ship it, then write it up.
```

Principles (three lines, readings are drafts):

```text
Position before submission. | Security before features. Lock the position down, then attack the problem.
Tap early, tap often. | Roll back early. A revert costs nothing; an outage does.
Leave your ego at the door. | Code review is not a fight. The best idea wins, not the loudest.
```

Phase 3 fields. Eras (five lines):

```text
2005-12-01 | white | 2 | Tamaso's MMA | Fairhope, AL | High school junior, working at Baumhower's Wings.
2018-01-30 | blue | 3 | Bagram BJJ | Bagram, Afghanistan | Veteran and contractor in Afghanistan, keeping Army helicopters in top shape as an aviation backshop mechanic.
2019-08-23 | purple | 1 | Bagram BJJ | Bagram, Afghanistan | Backshop manager leading multiple shops and 20+ personnel, and teaching BJJ to base personnel at night.
2020-09-23 | brown | 4 | Iron Grip BJJ | Fairhope, AL | Transitioning into tech through a University of Arizona Cyber Operations degree and the Coding Dojo coding bootcamp.
2025-12-09 | black | 0 | Championship Mixed Martial Arts | Daphne, AL | Software Engineer at Acentra Health, building healthcare software that integrates care management, utilization management, and prior authorization requests into a single system.
```

Now (four lines; Building and Home lab are drafts from your repo descriptions):

```text
Teaches | Adult no-gi, Tuesday and Thursday mornings, at Championship Mixed Martial Arts
Building | CalCrony: a self-hosted Discord bot for events and RSVPs, with reminders, ICS feeds and Google Calendar free/busy checks. The .NET 10 API is the product; the bot is just a client.
Home lab | Containerized, locked down, and built for high availability. This site runs on it.
Household | Wife, 5 kids, 6 dogs
```

Phase 4 fields:

| Field | Value |
|---|---|
| Desk photo alt (`OWNER_PHOTO_ALT` / Photo alt text) | `Joshua Mykitta in sunglasses and a tuxedo, looking slightly off to the left with a subtle, pensive smile` |
| Mat photo alt (`OWNER_PHOTO_FLIP_ALT` / Mat photo alt text) | `Joshua Mykitta, on the left, being promoted to Brazilian jiu-jitsu black belt by Rodney Souza, on the right` |
| Mat photo file | `aidlc-docs/inception/application-design/landing-directions/josh-mat.jpg` (720px downsample of `Promotion-black-belt-solo.jpg`; the full-resolution original stays on the owner's machine) |

For the production `.env` instead of the admin page, join lines with the literal `\n` (for example `SITE_NOW=Teaches | Adult no-gi, ...\nBuilding | CalCrony: ...`).
