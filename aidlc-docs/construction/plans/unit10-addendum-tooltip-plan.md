# Unit 10 addendum plan: BJJ wording for the theme-toggle tooltip (PR 1)

**Written**: 2026-09-05. **Owner decision**: "sure" (2026-09-04) to the wording "Switch to the white gi (light theme)" and "Switch to the black gi (dark theme)" (the owner corrected belt to gi on 2026-09-05 at approval). **Requirements**: section 4.1 of `inception/requirements/requirements-post-unit10-followups.md` (FR-B1 to FR-B5). **Workflow slot**: PR 1, right after the pinned header (PR 0, merged as PR #99). **Closes**: the deferred "Optional, owner's call" box of `unit10-bjj-landing-plan.md` Phase 5.

**Execution note**: XS (one attribute, one pure helper, a dozen lines of script, tests). The orchestrator implements it directly, as for PR 0, with the five-area review and the Copilot gate unchanged. Say so at approval if you want a phase agent instead.

## What the code looks like today (evidence)

- `Components/App.razor` renders `<html lang="en">` statically and already injects `SiteConfig Site`.
- `wwwroot/js/theme.js` loads synchronously in the head: `__applyTheme()` sets `data-theme` before the body exists (so the toggle button is not there yet); `__toggleTheme()` flips the theme and persists it. No tooltip logic.
- `wwwroot/js/site.js` (deferred) calls `__applyTheme()` in `onEnhancedLoad` after every enhanced navigation, because the merged server markup strips `data-theme` from `<html>`; the delegated `toggle-theme` action calls `__toggleTheme()`. site.js does not call `__applyTheme()` at startup.
- `Components/Layout/MainLayout.razor` lines 63-64: `<button class="theme-toggle" type="button" data-action="toggle-theme" title="Switch theme" aria-label="Switch between dark and light theme">`.
- `Services/SiteFlavor.cs` holds `SiteFlavorRules.Parse`, tested by a Theory in `SiteConfigTests`.
- `tests/Portfolio.Tests/Portfolio.Tests.csproj` links `app.css` and every `.razor` under `Components/` into the test output; `theme.js` is not linked yet.

## Non-negotiables

- Under `Default`: no attribute on `<html>` and the title untouched, so the plain page's HTML is byte-for-byte unchanged (FR-B1, NFR-10). A null attribute value makes Blazor omit the attribute entirely.
- The `aria-label` stays "Switch between dark and light theme" (FR-B4). No inline handler, no new script file, no CSS change (FR-B5). No owner fact (BR-18): the two sentences are fixed flavor wording.
- The strings are exactly the two approved sentences: "Switch to the white gi (light theme)" is shown while the page is dark (it names the target), "Switch to the black gi (dark theme)" while it is light.

## Steps

- [x] Branch `feat/theme-toggle-bjj-tooltip` from a realigned master; first commit `docs:` folding the pending aidlc-docs edits (the audit tail, the state file, the PR 0 plan ticks, the workflow-plan note, this plan). Done: docs commit a337392.
- [x] `Services/SiteFlavor.cs`: `SiteFlavorRules.HtmlDataFlavor(SiteFlavor flavor)` returns `"bjj"` for `Bjj` and `null` for `Default`, with a doc comment naming FR-B1 and the omit-when-null behaviour. Done (4ed0c67).
- [x] `Components/App.razor`: `<html lang="en" data-flavor="@SiteFlavorRules.HtmlDataFlavor(Site.Flavor)">` with a short comment: the attribute survives enhanced navigation because the server markup carries it, unlike `data-theme`, which theme.js re-applies. Done, with the fully qualified Portfolio.Web.Services.SiteFlavorRules, matching the file's other service references (the text-scan test pins that exact text).
- [x] `wwwroot/js/theme.js`: a `syncToggleTitle()` that returns unless `document.documentElement.dataset.flavor === 'bjj'`, finds `.theme-toggle` (returns if absent), and sets `title` to the black-gi sentence when the current theme is light, else the white-gi sentence. Called at the end of `__applyTheme` and `__toggleTheme`, and once on `DOMContentLoaded` because the head-time `__applyTheme()` runs before the button exists. Comments at the three call sites; `node --check` on the file. Done; node --check clean.
- [x] `tests/Portfolio.Tests/Portfolio.Tests.csproj`: link `wwwroot/js/theme.js` into the output as `js/theme.js` with the same `None` plus `CopyToOutputDirectory` idiom as `app.css`. Done.
- [x] Tests: `SiteConfigTests` gains a Theory for `HtmlDataFlavor` (`Bjj` gives `"bjj"`, `Default` gives null). New `ThemeToggleTooltipTests`: the linked `RazorComponents/App.razor` carries the `data-flavor="@SiteFlavorRules.HtmlDataFlavor(Site.Flavor)"` attribute; the linked `js/theme.js` contains each of the two sentences exactly once and the `dataset.flavor === 'bjj'` gate; the linked `RazorComponents/Layout/MainLayout.razor` still carries `title="Switch theme"` and `aria-label="Switch between dark and light theme"` on the toggle. Harness sanity checks name the csproj link when a file is missing, as `NoInlineOnClickTests` does. Done: 703 tests (698 plus the Theory's two rows and three Facts); Assert.Single replaced Assert.Equal on the counts after the xUnit2013 analyzer flagged them (59a02e7).
- [x] `dotnet build -warnaserror` with 0 warnings; `dotnet test` green (698 plus the new tests). Done: exit code 0, 0 warnings; 703 tests.
- [x] Local visual check (ephemeral Postgres, invented sample content, Browser pane, DOM measurements): with `SITE_FLAVOR=bjj`, `<html>` carries `data-flavor="bjj"`, the toggle's title reads the white-gi sentence while dark, flips to the black-gi sentence after a click and back after another, and after an enhanced navigation (home to `/blog` through the nav link) the title is still right; with the flavor unset, no `data-flavor` attribute and the title stays "Switch theme". Done 2026-09-05 (Browser pane DOM): BJJ run on localhost:5590: data-flavor bjj on html; first paint dark with the white-gi title and the unchanged aria-label; one toggle gives light and the black-gi title, another returns dark and the white-gi title; an enhanced navigation to /blog keeps the attribute and the right title; toggles on /blog work both ways. Default run: the served html element is exactly <html lang="en"> with no data-flavor anywhere in the page, and the title stays Switch theme through two toggles.
- [x] Five-area review (report-only) and remediation. Done 2026-09-05: security PASS; performance PASS (nit: the head script grew about 400 bytes gzipped, no action); correctness (minor: rendered probes added for the omitted attribute; nit: the App.razor pin now tolerates the namespace prefix); maintainability (minor: the App.razor comment now credits site.js for the re-apply; the pins narrowed to the attribute and scoped to the toggle button; a call-site comment added); framework (plausible blocker: the csproj Link used a backslash that MSBuild would not normalize on the Linux runner, now a forward slash). Build exit 0, 0 warnings; 705 tests.
- [ ] Push; PR `feat: BJJ wording for the theme-toggle tooltip`; Copilot gate per CONTRIBUTING.md; squash-merge; realign master stash-first; delete the branch; tick the Unit 10 plan's Phase 5 deferred box, this plan, the state file, the audit and memory.
- [ ] After the merge: release-please proposes the next minor; the owner bumps the compose image tag and recreates the container.

## Definition of done

Under the BJJ flavor the tooltip names the target theme in BJJ terms at first paint, after each toggle and after enhanced navigation; under the default flavor nothing changes; the accessible label is unchanged; build and tests green; PR merged with a clean Copilot pass.
