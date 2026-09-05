# Quick change plan: pinned site header (PR 0)

**Written**: 2026-09-04. **Owner request (verbatim)**: "make a quick change to let the navbar at the top stay locked at the top when a user scrolls down/up." **Requirements**: section 4.4 of `inception/requirements/requirements-post-unit10-followups.md` (FR-Q1 to FR-Q6). **Workflow slot**: PR 0, ahead of the tooltip PR (`inception/plans/workflow-planning-post-unit10-followups.md`, section 3).

**Execution note**: about a dozen CSS lines and one test, so the orchestrator implements it directly instead of briefing a phase agent (the precedent is the small Copilot follow-ups in Unit 10). The five-area review and the Copilot gate run exactly as for every other PR. Say so at approval if you want a phase agent instead.

## Non-negotiables

- `position: sticky`, never `fixed` (BR-13 stays true; the `AppCssTests` fixed-position facts stay green).
- No script change, no new animation or transition, no owner fact, theme tokens only for color.
- The plain landing page's HTML stays byte-for-byte unchanged (the change is CSS and a test).

## What the code looks like today (evidence)

- `.site-header { position: relative; border-bottom: 1px solid var(--border); }` with no background; `.site-header .inner` has `padding-block: 0.9rem`; the tallest child is the 38px burger, so the header is about 67px tall at the default font size.
- The 720px block keeps `.site-nav` as `position: absolute; top: 100%; z-index: 50` inside the header, so the header is already the panel's containing block.
- `html { scroll-behavior: smooth; }` (auto under reduced motion); no `scroll-padding` or `scroll-margin` anywhere; the game-plan nodes link to `#principles`.
- `body` and `main` set no overflow, so nothing between the header and the viewport would break stickiness.
- `.editor-preview { position: sticky; top: 1rem; }` in the admin post and theme editors would slide under a pinned header without an offset.
- `#blazor-error-ui` is the one `position: fixed` rule (`z-index: 1000`) and stays the only one.

## Steps

- [ ] Branch `feat/pinned-site-header` from a realigned master (stash-first); first commit `docs:` folding the pending aidlc-docs edits (audit, state, requirements, workflow plan, this plan, the owner checklist).
- [ ] `wwwroot/app.css`, Base: a layout constant `--header-h` on `:root` (the header height plus a 0.5rem gap; expected about 4.75rem, confirmed by measuring in the browser check). It is a layout constant like the belt colors, not a theme token: the 26-token catalog and its pinned tests do not change.
- [ ] `wwwroot/app.css`, Header: `.site-header` becomes `position: sticky; top: 0; z-index: 100; background: var(--bg);` keeping its `border-bottom`.
- [ ] `wwwroot/app.css`, Base: `html` gains `scroll-padding-top: var(--header-h);` next to `scroll-behavior`.
- [ ] `wwwroot/app.css`, admin editor: `.editor-preview` `top` becomes `var(--header-h)` so the preview panel clears the pinned header.
- [ ] Confirm the 720px block needs nothing: the panel still drops from the header's bottom edge; its `z-index: 50` now lives inside the header's stacking context, above the page.
- [ ] `tests/Portfolio.Tests/AppCssTests.cs`: one Fact, `SiteHeader_IsStickyAndAnchorTargetsClearIt`, built on `CssScanner.ParseLeafRules`: the `.site-header` leaf rule declares `position: sticky`, `top: 0` and a `z-index`; `:root` declares `--header-h`; the `html` rule declares `scroll-padding-top: var(--header-h)`; `.editor-preview` declares `top: var(--header-h)`.
- [ ] `dotnet build -warnaserror` with 0 warnings; `dotnet test` green (697 plus the new Fact).
- [ ] Local visual check in the Browser pane (`dotnet run` against an ephemeral, isolated Postgres container on a throwaway port, sample `SITE_*` values exported for the run only, `SITE_FLAVOR=bjj` so the anchor case exists): at 1440px dark and light, the header stays pinned scrolling down and up, opaque, border visible, content passing beneath; a game-plan node click lands `#principles` below the header; `/blog`, a post with code, `/projects`, `/contact`, `/signin` all keep the pinned header; at 390px, open the burger while scrolled, the panel attaches under the pinned header, closes on a second press, the theme toggle works; measure the header height and settle `--header-h`. The admin editor preview cannot be exercised without admin OAuth: the test pin covers it.
- [ ] Five-area review (report-only: correctness, security, framework, maintainability, performance) and remediation.
- [ ] Push; PR `feat: keep the site header pinned while scrolling`; Copilot gate per CONTRIBUTING.md; squash-merge; realign master stash-first; delete the branch; tick this plan, the state file, the audit and memory.
- [ ] After the merge: release-please proposes the next minor; the owner bumps the compose image tag in `Z:\docker\portfolio\docker-compose.yml` and recreates the container (the two commands from the checklist's Step 0).

## Definition of done

The header stays pinned on every page and width in both themes; anchor jumps and focus land below it; the mobile menu stays attached; nothing new is `position: fixed`; build and tests green; PR merged with a clean Copilot pass.
