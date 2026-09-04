# Unit 10 close-out handoff (2026-09-04)

**Purpose**: the single document a new session reads to pick this work up. It replaces the mid-construction handoff `unit10-session-handoff-2026-09-04.md` as the entry point; that file stays as the detailed record of mechanics and of the public surface after Phases 2 and 3.

**Read in this order**: this file; then `unit10-bjj-landing-plan.md` (the checkbox plan, now fully ticked apart from the owner actions and one deferred owner's-call item); then, only if you need design detail, `unit10-bjj-landing-handoff.md` (the design brief with the seven locked decisions) and the functional design under `aidlc-docs/construction/unit10-bjj-landing/functional-design/`. Log every owner input in `aidlc-docs/audit.md` and keep `aidlc-docs/aidlc-state.md` current.

## What shipped

Unit 10 turned the landing page into an opt-in BJJ-themed layout (`SITE_FLAVOR=bjj`), edited at `/admin/site`, with the default landing page byte-for-byte unchanged when the flag is unset.

| Phase | PR | Squash commit | Tests after | What |
|---|---|---|---|---|
| 1 `test:` | #89 | `c1380a2` | 445 | `HtmlRenderer` render tests for `LandingSections`, the `CssScanner`, the fixed-position invariant |
| 2 `feat:` | #90 | `e6a4a75` | 567 | `SiteFlavor`, hero game plan, rank bar, principles, admin copy fields, migration `AddBjjLandingCopy`, ADR 0002 (belt colors as fixed constants) |
| 3 `feat:` | #92 | `7dddfdf` | 666 | The road (belt ladder and era table) and Now (tiles), migration `AddRoadAndNow`, BR-9 check on effective values, reduced-motion block made to win the cascade with `!important` |
| 4 `feat:` | #94 | `18b1788` | 689 | Second owner-photo slot (`OwnerPhotoSlot.Flip`, `/owner-photo-flip`, `OwnerPhotoFlipAlt`, migration `AddOwnerPhotoFlipAlt`), hero two-photo switch, `fetchpriority` on the hero image |
| 5 `perf:` | #96 | see `git log` on master | 697 | Fraunces preload, delegated `data-action` clicks (no inline `onclick` left, pinned by a test), README, `CONTEXT.md` glossary, test instructions reconciled (690 after the phase agent, 697 after the Copilot round that pinned the onclick regex with a Theory) |

Releases 1.23.0 (`027e862`) and 1.24.0 (`e2c7c05`) landed on master between the phases; release-please will propose the next one from the `feat:`/`perf:` squash titles.

## Where to pick up

Unit 10 is complete once PR #96 is merged. Candidates for the next session, in priority order:

1. **Owner actions after deploy (no code; needs the owner)**: set `SITE_FLAVOR=bjj` in the production `.env`; paste the content-sheet rows from the plan at `/admin/site` (hero heading, eyebrow, tagline, game plan, belt caption and degrees, principles, eras, now); set `OWNER_PHOTO_FLIP_FILE` on the same read-write mount as the primary photo, upload the mat portrait and paste both alt texts; optionally align the local `.env` (plan Phase 0). Also on the owner's machine: turn the `python3.exe` app-execution alias off once in Windows Settings (see "Tooling" below).
2. **Deferred owner's call** (plan, Phase 5): under `Bjj`, the theme toggle's `title` could read "Switch to the white gi (light theme)" / "Switch to the black belt (dark theme)" (set by `theme.js` from a `data-flavor` attribute on `<html>`; the `aria-label` stays functional). Small change; implement only if the owner asks.
3. **Recorded follow-ups** (plan, "Out of scope"): a "current belt" field so the rank bar can draw a non-black belt; per-node landing sections for Guard, Pass, Mount, Submit; kids' and coral belts; security headers and rate limiting as separate units; a longer cache lifetime for the un-fingerprinted `/fonts/*` route (hourly revalidation today).
4. **State housekeeping**: after #96 merges, the main checkout at `C:\Users\joshu\source\repos\Portfolio` may hold an uncommitted `aidlc-docs/audit.md` append recording the merge; fold it into your first `docs:` commit, or recreate it from the table above and `git log`.

## The execution model that worked (owner instruction, 2026-09-04)

- One fresh **Sonnet** general-purpose subagent per phase, briefed with a self-contained prompt (plan section, functional-design rules, design source files to copy from, code to change, tests to write, definition of done, report format). It works on the phase branch, gets `dotnet build -warnaserror` to 0 warnings and `dotnet test` green, ticks its plan boxes, commits by explicit path, never pushes. The briefs are in this folder (`unit10-phase3-brief.md`, `unit10-phase4-brief.md`, `unit10-phase5-brief.md`) and are the templates to copy.
- Five **review** subagents in parallel, report-only: correctness and bugs, security (with a BR-18 grep for owner facts), framework awareness, maintainability, performance. Fixed report format (verdict, severity, file:line, failure scenario, evidence, fix, CONFIRMED or PLAUSIBLE).
- Remediation: a fresh subagent for a large list (Phase 3), or the orchestrator directly for a handful of one-line fixes (Phases 4 and 5).
- Then push, PR with the plan's title, Copilot gate per `CONTRIBUTING.md` (fix or reply on each thread, re-request until a pass adds no actionable comments), squash-merge, realign master, update plan, state, audit, memory.
- Owner facts never go into code, tests, `.env.example` or docs examples (BR-18); every phase's fixtures are invented.

## Mechanics (the short list; details in `unit10-session-handoff-2026-09-04.md`)

- **gh**: run every command as `env -u GITHUB_TOKEN gh ...` (the session env token is invalid; the keyring login JJWren works). Copilot is not auto-requested: `env -u GITHUB_TOKEN gh api -X POST repos/JJWren/Portfolio/pulls/N/requested_reviewers -f "reviewers[]=copilot-pull-request-reviewer[bot]"`; it reviews three to five minutes later. Merge: `gh pr merge N --squash --subject "<title> (#N)" --body "..."`.
- **Realign after a merge, in this order**: `git stash push aidlc-docs/audit.md` (and any other uncommitted orchestrator docs), `git switch master`, `git fetch origin`, `git reset --hard origin/master`, `git stash pop`, `git branch -D <branch>`. Never skip the stash: `git switch` aborts on an uncommitted audit append and a following `reset --hard` silently discards it (three entries had to be restored on 2026-09-04).
- **Long markdown goes through the Write tool**, not a Bash heredoc (the shell wrapper failed on a large brief). Audit entries are appended with `printf ... >> audit.md` using CRLF and no apostrophes or backticks in the text.
- **Migrations**: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project src/Portfolio.Web --startup-project src/Portfolio.Web` (placeholder `SITE_OWNER_NAME`, `CONTACT_EMAIL`, `ConnectionStrings__Default` if the design-time host needs them).
- **Local visual checks**: `dotnet run` against an ephemeral, isolated Postgres container on a throwaway port with sample values exported for the process only; stop processes by PID (a blanket `taskkill /IM dotnet.exe` once killed an unrelated process); reduced-motion emulation and admin OAuth are not available locally.
- **Tooling**: the `security-guidance` plugin's Stop, commit and push hooks used to fail in a loop because its shim picked `python3`, a Windows app-execution alias for the Store "Python Install Manager" that cannot see `%APPDATA%\Claude`. Fixed by a `python3.12.exe` copy in the python.org 3.12 install (which the shim now prefers) and by removing the alias file; the durable step is the Settings toggle. Post-mortem: `~/Downloads/claude-code-security-hook-loop-2026-09-04.md`. A `metrics` JSON line with `"skipped": true` from the plugin is normal.

## Public surface after Phase 5 (for briefing future work)

- Phase 2 and 3 surfaces: see the "Public surface" sections of `unit10-session-handoff-2026-09-04.md` (`SiteFlavor`, `BjjRules` with `Belt`, `Era`, `Rung`, `NowItem`, the parsers and validators, `SiteConfig` trailing members, `SiteContentRules.Validate(draft, SiteConfig?)`, `EffectiveSiteContent` never-null collections, the `GamePlan`, `RankBar`, `BeltBand`, `Road` components, the `Landing (BJJ flavor)` banner in `app.css` with exactly one 720px block, the single reduced-motion block whose declarations all carry `!important`).
- Phase 4: `OwnerPhotoSlot { Primary, Flip }`; `OwnerPhotoService.IsConfigured(slot)` (a method now), `GetVersionedUrl(slot)`, `SaveAsync(stream, slot, ct)`, `Delete(slot)`; `/owner-photo-flip` served by the same handler as `/owner-photo`; `SiteConfig.OwnerPhotoFlipFile`, `OwnerPhotoFlipAlt`; `SiteContent.OwnerPhotoFlipAlt`; `SiteContentDraft` ends `..., ErasText, NowText, OwnerPhotoFlipAlt` (all positional, no defaults; named arguments at every construction site); `EffectiveSiteContent.OwnerPhotoFlipAlt`; the hero switch markup in `LandingSections.razor` (the desk tile image deliberately carries no `owner-photo` class); test harness `LandingRenderHarness.MaximalConfig(primaryFile, flipFile)`.
- Phase 5: `site.js` handles `button[data-action]` clicks (`toggle-nav`, `toggle-theme`, `scroll-projects` with `data-direction`); `__toggleNav` and `__scrollProjects` are gone; `theme.js` unchanged; `App.razor` preloads `fonts/fraunces-latin.woff2`; `tests/Portfolio.Tests/NoInlineOnClickTests.cs` scans every `.razor` under `Components` (linked into the test output by a csproj glob with `LinkBase`).

## Memory notes

`~/.claude/projects/C--Users-joshu-source-repos-Portfolio/memory/`: `portfolio-unit10-bjj-landing.md` (project status; mark Unit 10 complete when #96 is merged), `portfolio-orchestration-model.md` (the owner's execution model), `joshua-mykitta-bjj-facts.md` (owner facts; content only, never code).
