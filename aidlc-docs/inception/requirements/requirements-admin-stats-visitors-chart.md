# Requirements: daily-visitors chart on the admin stats page (Unit 14)

**Written**: 2026-09-11, in the same session as the Unit 13 requirements, while that unit's approval was open. **Owner and only stakeholder**: Joshua Mykitta. **Depth** (adaptive): standard; grilling round 3 put eight decisions to the owner, who answered the queue position (decision 23) and left the other seven to the recommended answers, taken as assumptions and confirmed at the approval gate. **Sources**: `src/Portfolio.Web/Components/Admin/Stats.razor` (the period select, the tiles, the section order, the reload handler); `Services/AnalyticsService.cs` (the summary query and how today is merged live); `Data/DailySiteStat.cs` and `Services/AnalyticsRollup.cs` (one row per rolled-up day, zero days included); `wwwroot/app.css` (the stats styles and the theme tokens); `CONTEXT.md` (Daily Visitor, Rollup, Watermark); the dataviz skill (form, marks, interaction, anti-patterns); `docs/adr/0001-first-party-cookieless-analytics.md`. Section 5 of `requirements.md` (NFR-1 to NFR-9) applies, in particular NFR-1 (no interactivity beyond CSS or small first-party script on public pages; this page is an admin page), NFR-2 (no CDN, no third-party script), NFR-6 (motion) and NFR-7 (accessibility).

## 1. The ask (verbatim)

```text
I would like to create one more add request. In the admin/stats view, above Top Pages but below the stat blocks, I would like a graph that changes based on the dropdown (7 days, 30 days, 90 days, 365 days) that is a line graph which shows the daily visitors.
```

Round 3 answer: "Q23) resume first, then this one, then unit 11 and 12".

## 2. Intent analysis

- **The metric already has a name**: the glossary's Daily Visitor, one distinct Visitor Key on one UTC day. The tile shows the sum over the Period; the chart shows the per-day series behind that sum, so the two must agree for the same Period.
- **The dropdown is the existing period select.** It also offers All time, which the ask did not list; decision 21 covers it.
- **Placement is fixed by the ask**: after the stat tiles, before the Top pages heading.
- **Change over time is a line chart's job** (dataviz, choosing a form). One series, so no legend; the heading names it.
- **The site's rules shape the how**: no CDN and no third-party script, so the chart is server-rendered inline SVG, the same technique as the inline icons.

## 3. Current state (facts found before round 3)

| Fact | Where |
|---|---|
| The period select offers 7, 30, 90, 365 and All time; changing it runs one reload that refreshes every section. | `Components/Admin/Stats.razor` |
| The Period is the last N UTC days ending today, today included; All time starts at the first recorded day. | `Stats.razor` (`PeriodRangeAsync`), `AnalyticsService.FirstDayAsync` |
| Rolled-up daily rows carry a visitors count for every day up to the Watermark, zero-traffic days included. | `Data/DailySiteStat.cs`, `Services/AnalyticsRollup.cs` |
| Today's visitors are computed live as the distinct visitor keys in the raw page views since midnight UTC; the tiles merge that with the rolled-up rows. A past day with no rolled-up row (the 20 minutes before the 00:20 rollup) counts as zero in the tiles. | `Services/AnalyticsService.cs` (`GetSummaryAsync`) |
| No per-day series query exists yet. | `Services/AnalyticsService.cs` |
| The page is an interactive admin page; the only rendered SVG precedent is the inline icon glyphs. | `Stats.razor`, `Components/Icon.razor` |
| Theme tokens available: the four brand colors, `--accent` (gold), `--border`, `--text`, `--text-muted`, `--surface`; the light theme overrides them under `:root[data-theme='light']`, and the admin theme editor can override the catalog. | `wwwroot/app.css` |
| Tests have no database harness: analytics logic is tested as pure functions (the rollup aggregation) and components through HtmlRenderer with fixed inputs. | `tests/Portfolio.Tests/AnalyticsRollupTests.cs`, `LandingSectionsRenderTests.cs` |

## 4. Unit of work

| Unit | Title | Commit type | Size | Depends on |
|---|---|---|---|---|
| Unit 14 | Daily-visitors chart on the admin stats page | `feat:` | S, one PR, no migration | after Unit 13, before Units 11 and 12 (owner, decision 23) |

Runs under the orchestration model recorded on 2026-09-04. Before any chart code is written the phase agent loads the dataviz skill and follows its procedure (form, color by job, validate, marks, hover layer, accessibility pass, render and look).

## 5. Functional requirements

| Id | Requirement |
|---|---|
| FR-V1 | A "Daily visitors" heading (h2) with the caption "one point per UTC day", placed after the stat tiles and before the Top pages heading. The chart re-renders with every period change through the existing reload; no new control. |
| FR-V2 | A new analytics query returns one point per UTC day for the Period: the rolled-up row's visitors for each past day (a day with no row draws as zero, the tiles' own blind spot), and today's value computed live exactly as the summary does. The points sum to the tile's Daily visitors figure for the same Period. |
| FR-V3 | The x-axis starts at the later of the period start and the first recorded day and ends today; under All time it starts at the first recorded day. One point per day at every length; no bucketing. |
| FR-V4 | Today is the last point, drawn as a hollow marker at the end of a dashed final segment; its tooltip reads "today so far". |
| FR-V5 | The chart is inline SVG emitted by a Razor component from the point list: no script, no external resource, full container width through a viewBox with a fixed aspect, and a minimum height on narrow screens. |
| FR-V6 | Axes: a zero baseline; three to five round-number horizontal gridlines with labels; about five evenly spaced date labels plus the first and last day, in a short month-and-day form; gridlines and axis text recessive in the border and muted-text tokens. No per-point markers except today's and the hovered day's. |
| FR-V7 | The line is 2px wide in one brand token, so the light theme and the admin theme overrides recolor it. The token is chosen at construction by running the dataviz validator against the dark and the light chart surfaces; text and labels wear text tokens, never the series color; one series, so no legend. |
| FR-V8 | A CSS-only hover and keyboard-focus layer inside the SVG: each day has an invisible hit column wider than the mark; hovering or focusing it shows a crosshair, the day's marker and a tooltip with the date and the count, kept inside the chart bounds. No script. Addendum (2026-09-11, Copilot round 2 on PR #106): where the spacing between points is narrower than the mark (365 days: about 1.8 viewBox units), the hit column equals the spacing so a day never steals the pointer from its neighbour; a wider target would overlap. Keyboard focus and the data table stay exact at every period. |
| FR-V9 | A collapsible data table under the chart (a details element titled "Data table") with one row per day, date and visitors, carrying the same numbers the line draws. The table is the assistive-tech reading of the chart, as with The Road. |
| FR-V10 | Empty state: with fewer than two days of data in the Period the chart area shows "Not enough data to chart yet." and the table is omitted. |
| FR-V11 | No motion: no transition or animation on the line, the marker or the tooltip (NFR-6). |
| FR-V12 | Tests: the scale, tick, path and range-start computations as pure functions with unit tests (round-number ticks, zero baseline, the dashed final segment, the two-day threshold, the later-of-two start rule, the sum equals the tile); one rendered HtmlRenderer test of the chart component with fixed data pinning the heading, the table rows, the dashed final segment and the absence of script and external references. The new query follows the existing analytics query style and is exercised by the local smoke check, since no database harness exists. |
| FR-V13 | Glossary: "Period" added to `CONTEXT.md` under Analytics (written 2026-09-11 as an assumption of decision 24). |

## 6. Non-functional requirements (in addition to NFR-1 to NFR-9)

| Id | Requirement |
|---|---|
| NFR-22 | No new NuGet package, no script, no CDN, no font or image request added by the chart. |
| NFR-23 | Accessibility (NFR-7): the heading names the chart, the hit columns are focusable in date order, the tooltip is visible on focus as well as hover, and the data table gives screen-reader users every value; contrast for axis text follows the text tokens. |
| NFR-24 | At most a few hundred points: one path element for the line, one hit column per day, nothing else per point. Addendum (2026-09-11, Copilot round 3 on PR #106): under All time the series grows one point per calendar day without bound, by decision 21 (no bucketing); a site several years old would exceed a few hundred points, at which time weekly bucketing beyond a length becomes a follow-up. |
| NFR-25 | Public pages are untouched; only the admin stats page changes. |
| NFR-26 | `dotnet build -warnaserror` with 0 warnings; `dotnet test` green plus the new tests; one PR sized for the Copilot gate with a title from the CI allow-list. |

## 7. Decisions from grilling round 3

| # | Decision | Chosen | Status |
|---|---|---|---|
| 17 | How the chart is drawn | Razor-rendered inline SVG, no script | recommended, taken |
| 18 | What the line shows | Daily visitors only, no legend | recommended, taken |
| 19 | Today | Included as a hollow marker on a dashed final segment, "today so far" | recommended, taken |
| 20 | A site younger than the period | Axis starts at the later of period start and first recorded day | recommended, taken |
| 21 | All time | One point per day, no bucketing | recommended, taken |
| 22 | Reading a value | CSS-only hover and focus tooltip plus a collapsible data table | recommended, taken |
| 23 | Queue position | Unit 13 first, then this unit, then Units 11 and 12 | owner |
| 24 | Glossary | "Period" added; avoid range, window, timeframe | recommended, taken |

No ADR: nothing here is hard to reverse or surprising given the inline-SVG icon precedent and the no-CDN rule.

## 8. Decisions taken by default (say so if you disagree)

1. The heading and caption wording in FR-V1.
2. The axis treatment in FR-V6 and the zero baseline.
3. The series color is picked and validated at construction, not chosen here (dataviz: color comes last and is computed).
4. The empty-state wording and the two-day threshold in FR-V10.
5. A missing rolled-up day draws as zero (consistent with the tiles) rather than as a gap.
6. Chart sizing: full width, fixed aspect, minimum height on narrow screens; the tooltip stays inside the chart.

## 9. Owner actions

None. No env, compose or deploy step; the release deploy carries it.

## 10. Out of scope (recorded)

Page views as a second line; weekly bucketing; script-driven tooltips or zoom; export or download of the data; per-route or per-referrer charts; a sparkline on the dashboard card.

## 11. Approval

Pending, together with Unit 13. "Requirements analysis complete. Do you want to request changes or continue to the next stage?"

**Approval (owner, 2026-09-11)**: "A", both documents as written, the seven assumed answers included.
