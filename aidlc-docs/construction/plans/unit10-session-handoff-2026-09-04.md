# Unit 10 — session handoff (construction, 2026-09-04)

**Written**: 2026-09-04 by the orchestrating session after Phases 1 and 2. **Read first**: this file, then `unit10-bjj-landing-plan.md` (the checkbox plan; pick up at the first unchecked box), then `unit10-bjj-landing-handoff.md` (the design brief and the seven locked decisions). Log every owner input in `aidlc-docs/audit.md` and keep `aidlc-docs/aidlc-state.md` current.

## Where the work stands

| Phase | State |
|---|---|
| 0 owner actions | Closed: the demo post stays published (owner decision); the drafts ship as drafts. Optional local `.env` alignment still open. |
| 1 `test:` render safety net | Merged 2026-09-04 as PR #89, squash `c1380a2`. `tests/Portfolio.Tests/Support/LandingRenderHarness.cs` and `CssScanner.cs` are the shared helpers every later phase extends. |
| 2 `feat:` foundation | Merged 2026-09-04 as PR #90, squash `e6a4a75`. Five-area internal review, remediation, Copilot rounds 1 to 3 (three robustness fixes, one tail-field parsing fix, then a pass with no new comments). 567 tests. |
| 3 `feat:` The road and Now | Merged 2026-09-04 as PR #92, squash `7dddfdf`. Five-area review found three real problems (owner facts in fixtures, BR-9 check on draft-only values, reduced-motion block losing the cascade) fixed by a remediation agent; Copilot round 1 had zero comments. 666 tests. |
| 4 `feat:` second photo slot and portrait switch | In progress since 2026-09-04 on `feat/owner-photo-flip` (docs commit `1523835`); brief: `unit10-phase4-brief.md`. |
| 5 `perf:` close-out | Not started. |

Owner actions after the Phase 2 image deploys: set `SITE_FLAVOR=bjj` in the production `.env`, then paste the Phase 2 rows of the plan's content sheet at `/admin/site` (hero heading, eyebrow, tagline, game plan, belt caption, degrees, principles). Two visual checks were not possible locally and are worth an owner glance after deploy: reduced-motion behavior (covered by the CSS invariant tests) and the `/admin/theme` preview frame with the flavor on.

## Working tree at handoff

Pattern: after each merge the plan gate box, `aidlc-docs/aidlc-state.md` and `aidlc-docs/audit.md` are edited on master and left uncommitted; the next phase branch starts with a `docs:` commit carrying them plus that phase's brief, and a second `docs:` commit lands before the PR opens with the review trail. Never let a subagent stage, stash or revert these files; every brief says so.

## The execution model (owner instruction, 2026-09-04)

- One fresh **Sonnet** general-purpose subagent per phase, briefed with a self-contained prompt (plan section, functional design rules, design source files to copy from, code to change, tests to write, definition of done, report format). It works on the phase branch, gets `dotnet build -warnaserror` to 0 warnings and `dotnet test` green, ticks its plan boxes, commits by explicit path, and never pushes.
- When it reports, five **review** subagents run in parallel, report-only: correctness and bugs, security, framework awareness, maintainability, performance. Each gets the branch, the specs, an area focus list, and a fixed report format (verdict, findings with severity, file:line, failure scenario, evidence, fix, CONFIRMED or PLAUSIBLE).
- Findings are consolidated into one remediation brief for a fresh subagent (SendMessage is not available in this environment). Small Copilot follow-ups can be applied by the orchestrator directly.
- Then: orchestrator re-verifies build and tests, pushes, opens the PR with the plan's title, waits for Copilot, fixes, replies on each thread, re-requests, repeats until a pass adds no actionable comments, squash-merges, realigns local master, updates plan, state and audit.
- Owner facts never go into code, tests or `.env.example` (BR-18); reviewers check for it with a grep of the content-sheet phrases over the branch diff.

## Mechanics that cost time to discover

- **gh authentication**: the `GITHUB_TOKEN` in the session environment is an invalidated fine-grained PAT. Run every gh command as `env -u GITHUB_TOKEN gh ...` so gh uses the keyring login (JJWren, scopes gist, read:org, repo, workflow), which can create PRs, comment, re-request Copilot and squash-merge. `git push` works as is.
- **Copilot re-review**: `env -u GITHUB_TOKEN gh api -X POST repos/JJWren/Portfolio/pulls/N/requested_reviewers -f "reviewers[]=copilot-pull-request-reviewer[bot]"`. `gh pr edit --add-reviewer` cannot resolve the bot. Copilot reviews about three to four minutes after a PR opens or a re-request; the timeline shows `review_requested` then `copilot_work_started`.
- **Waiting without polling by hand**: a background Bash `until` loop on `gh pr view N --json reviews` counting Copilot reviews (30 s sleeps, 15-minute cap) wakes the session when the review lands.
- **Reading a review**: `gh pr view N --json reviews` for the verdict, `gh api repos/JJWren/Portfolio/pulls/N/comments` for inline comments (reply with `POST .../comments/{id}/replies -f body=...`), `gh pr checks N --json name,state` for CI.
- **Merge**: `env -u GITHUB_TOKEN gh pr merge N --squash --subject "<PR title> (#N)" --body "..."`; the remote branch is auto-deleted. Then realign: `git stash push aidlc-docs/audit.md` (and any other orchestrator docs), `git switch master`, `git fetch origin`, `git reset --hard origin/master`, `git stash pop`, `git branch -D <branch>`.
- **Migrations**: `dotnet tool restore` then `dotnet ef migrations add <Name> --project src/Portfolio.Web --startup-project src/Portfolio.Web`, with placeholder `SITE_OWNER_NAME`, `CONTACT_EMAIL` and `ConnectionStrings__Default` environment values if the design-time host needs them.
- **Local visual checks**: the Phase 2 agent ran `dotnet run` against an ephemeral Postgres container with sample `SITE_*` values exported for the process only, and used the Browser tools at 1440 and 390 widths; it cleaned up afterwards. Reduced-motion emulation and admin OAuth are not available locally.
- **Hook noise (resolved 2026-09-04)**: the `security-guidance` plugin's shim (`sg-python.sh`) picked `python3`, which on this machine is a Windows app-execution alias for Python 3.14; a process launched through that alias cannot see `%APPDATA%\Claude`, so every Stop, commit and push hook failed with "can't open file ... security_reminder_hook.py" and re-woke the session in a loop. Fix applied: `python3.12.exe` (a copy of `python.exe`) added to `%LOCALAPPDATA%\Programs\Python\Python312`, which the shim now picks first. Durable fix for the owner: turn off the `python3.exe` app execution alias in Windows Settings. Post-mortem: `~/Downloads/claude-code-security-hook-loop-2026-09-04.md`. A `metrics` JSON line with `"skipped": true` from the plugin is normal.
- **Shell quoting**: long markdown goes through the Write tool, not Bash heredocs (Git Bash quoting failed on one). Audit entries are appended with a `cat <<EOF | sed ... >> audit.md` block using CRLF line endings and no backticks or apostrophes inside.

## Public surface after Phase 3 (for briefing Phases 4 and 5)

Added by Phase 3 on top of the Phase 2 surface below: `BjjRules.Belt` (`White, Blue, Purple, Brown, Black`), `ParseBelt`, `CssName`, `DateFormat`, records `Era(Date, Belt, Stripes, Gym, Location, Role)`, `Rung(Belt, Stripes)`, `NowItem(Label, Value)`, `ParseEras`, `Rungs`, `ParseNow`, `ValidateEras`, `ValidateNow`, `ValidateDegreesAgainstEras(degrees, eras, degreesSource?, erasSource?)`, constants `MaxEras = 12`, `MaxNowItems = 8`; `SiteConfig.EraLines`, `NowLines` (`SITE_ERAS`, `SITE_NOW`); `SiteContent.Eras`, `Now` (migration `20260904182920_AddRoadAndNow`); `EffectiveSiteContent.Eras`, `Rungs` (resolved once), `Now`; `SiteContentDraft` with twelve positional fields ending `PrinciplesText, ErasText, NowText`; `SiteContentRules.Validate(draft, SiteConfig? site = null)` (BR-9 on effective values); `Components/BeltBand.razor(Belt, Stripes)`, `Components/Road.razor(Eras, Rungs)`; the reduced-motion block carries `!important` on every declaration (tested); one 720px block per banner section (tested).

### Phase 2 surface

- `Services/SiteFlavor.cs`: `SiteFlavor { Default, Bjj }`, `SiteFlavorRules.Parse(string?)`.
- `Services/BjjRules.cs`: records `GamePlanNode(Term, Reading, How)`, `Principle(Maxim, Reading)`; `SplitLines`, `SplitFields`, `BoundLineLength` (truncates), `ParseGamePlan`, `ParsePrinciples` (lenient, capped), `ClampDegrees`, `ValidateGamePlan`, `ValidatePrinciples`, `ValidateDegrees`; constants `MaxDegrees = 6`, `GamePlanNodeCount = 4`, `MaxPrinciples = 6`, `MaxLineLength = 500`. The `Belt` enum, eras and now are Phase 3.
- `Services/SiteConfig.cs`: trailing optional `Flavor`, `HeroEyebrow`, `GamePlanLines`, `BeltCaption`, `BeltDegrees`, `PrincipleLines`.
- `Services/SiteContentRules.cs`: `EffectiveSiteContent` gained `HeroEyebrow`, `GamePlan` (never null), `BeltCaption`, `BeltDegrees`, `Principles` (never null); `SiteContentDraft` record; `Validate(draft)` (editor field order); `LinesText`, `ParseLines` (delegates to `BjjRules.SplitLines`), `ParseDegrees` (invariant culture); `Resolve` truncates and caps leniently.
- `Services/SiteContentService.cs`: `SaveAsync(SiteContentDraft)`; internal `SiteContentValues` record feeds the upsert.
- `Data/SiteContent.cs` and migration `20260904145755_AddBjjLandingCopy`: `HeroEyebrow` varchar(120), `GamePlan` text[], `BeltCaption` varchar(200), `BeltDegrees` int null, `Principles` text[].
- `Components/GamePlan.razor` (`Nodes`; renders nothing unless exactly four), `Components/RankBar.razor` (`Degrees` clamped, `Caption`), `Components/LandingSections.razor` (flavor gate; `Default` branch unchanged), `Components/Admin/SiteContentEditor.razor` ("Mat" group under the flavor).
- `wwwroot/app.css`: seven `:root` constants (ADR 0002), `.visually-hidden`, the `Landing (BJJ flavor)` banner section, the single reduced-motion block extended.
- Tests: `tests/Portfolio.Tests/Support/LandingRenderHarness.cs` (`RenderAsync`, `RenderGamePlanAsync`, `BuildConfig`, `BuildContent`, `MaximalConfig`, `MaximalContent`: extend the maximal pair in every phase), `Support/CssScanner.cs` (`ParseLeafRules`, `RulesInside`, `SubjectSelectorTokens`, `ExtractBannerSection`), `BjjRulesTests`, `AppCssTests`, `LandingSectionsRenderTests`, `SiteConfigTests`, `SiteContentRulesTests`. Fixtures use invented copy (Warm-up/Drill/Roll/Rest, "Sample heading").

## Memory notes

`~/.claude/projects/C--Users-joshu-source-repos-Portfolio/memory/`: `portfolio-unit10-bjj-landing.md` (project status; update the Phase lines), `portfolio-orchestration-model.md` (the owner's execution model), `joshua-mykitta-bjj-facts.md` (owner facts; content only, never code).

## Status line

Updated by the orchestrating session at wrap-up: see the last audit entry in `aidlc-docs/audit.md` and the Unit 10 block in `aidlc-docs/aidlc-state.md` for whether PR #90 merged before the session ended.
