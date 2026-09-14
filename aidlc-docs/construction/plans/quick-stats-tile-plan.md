# Quick change plan: average per day on the Daily visitors tile (PR 1.5)

**Owner decision** (grilling 2026-09-13, Q3: A): a quick `fix:` PR ahead of Unit 11. **Why**: the "Daily visitors" tile on `/admin/stats` sums each UTC day's unique visitors over the Period, while the chart below it, under the same title, shows each day; on 2026-09-12 the owner read the sum as a per-day figure ("why does daily visitors state 365... it never stops 20"). **Requirement**: FR-S1 to FR-S3 in section 10 of `inception/requirements/requirements-post-unit10-followups.md`. **Branch** `fix/stats-daily-visitors-average`; **PR title** `fix: show the average per day on the daily visitors tile`. **Execution**: orchestrator-implemented with the five-area review, as PR 0 was; Copilot gate; squash-merge. **Release**: rides with Unit 11 and 12a (owner decision Q2, B).

## Non-negotiables

- The Period stays one thing for the tiles, the tables and the chart (`CONTEXT.md`, Period); today's partial day is included, as the term says.
- No change to what the analytics pipeline stores or to the chart (ADR 0001, NFR-15); no owner fact in tests (BR-18).
- `dotnet build -warnaserror` at 0 warnings; `dotnet test` green, gated on the summary line.

## What the code looks like today (evidence)

- `Components/Admin/Stats.razor` lines 41 to 44: the tile renders `@_summary.DailyVisitors.ToString("N0")` over the label "Daily visitors"; line 17 the note "Visitors are unique per UTC day by design"; `_period` ("7", "30", "90", "365", "all"), `_firstDay`, `PeriodRange(_firstDay)` at 196 giving `(_rangeFrom, _rangeTo)` with `from = today.AddDays(1 - days)` or the first recorded day for all time; `LoadAsync` at 178 fetches `StatsSummary` and re-derives today's chart point from the tile (`WithTodayFromTotal`).
- `Services/AnalyticsService.cs` line 9: `record StatsSummary(int Views, int DailyVisitors, int ContactSubmits, int ProjectClicks, int ResumeDownloads)`; `FirstDayAsync` at 144; `GetSummaryAsync(from, to)` at 154.
- `Components/Admin/VisitorsChart.razor` line 12: the chart's `<h2>` reads "Daily visitors" (unchanged by this plan).
- `Services/VisitorsChartRules.cs` and `AnalyticsRules.cs`: pure helpers with their own test fixtures (`VisitorsChartRulesTests`, `AnalyticsRulesTests`).

## Steps

- [ ] Branch `fix/stats-daily-visitors-average` from a realigned master (stash-first); first commit `docs:` folding every pending aidlc-docs edit (audit, state, handoff, the grilling amendments in the requirements and the workflow plan, `CONTEXT.md`, the Unit 11 functional design and plan, this plan).
- [ ] `Services/AnalyticsRules.cs`: `PeriodDays(DateOnly from, DateOnly to)` (inclusive count, at least 1) and `AveragePerDay(int visitorDays, int days)` (whole number, half away from zero, 0 when `days` is 0), with summaries citing the Period term.
- [ ] `Components/Admin/Stats.razor`: the tile's `stat-value` shows `AveragePerDay(_summary.DailyVisitors, PeriodDays(_rangeFrom, _rangeTo))`; a `stat-sub muted` line under the label reads "354 visitor-days over 30 days" ("1 day" when singular), numbers formatted "N0"; the label and the chart title stay "Daily visitors".
- [ ] `wwwroot/app.css`: a `.stat-sub` rule only if no existing small muted style fits the tile (check `.stat-tile` and `.stat-label` first); nothing in the reduced-motion block.
- [ ] Tests: `AnalyticsRulesTests` for the two helpers (a 30-day period, an all-time period from a first day to today, a single day, zero visitor-days, rounding at the half); `construction/build-and-test/unit-test-instructions.md` totals refreshed.
- [ ] `dotnet build -warnaserror` with 0 warnings; `dotnet test` green (gate on the summary line). The page cannot be rendered locally without admin sign-in: the pure tests carry the arithmetic and the owner sees the tile after the 12a deploy.
- [ ] Five-area review (report-only) and remediation; build and tests green after every applied finding.
- [ ] Push; PR `fix: show the average per day on the daily visitors tile`; Copilot gate per CONTRIBUTING.md; squash-merge; realign master stash-first; delete the branch; tick this plan, the state file, the audit and memory; report to the owner (no stop).

## Definition of done

Every box ticked; 0 warnings; tests green; the PR merged; the tile reads as an average per day with the visitor-days and the day count beneath it; the chart and the stored data untouched.

## Deviations

(none yet)
