# Unit Test Instructions

```bash
dotnet test                          # all tests
dotnet test --filter SlugHelperTests # one fixture
```

## Coverage (837 tests, 46 fixtures — as of the Daily visitors tile quick fix (PR 1.5), on top of Unit 14, the admin stats daily-visitors chart)
| Area | Fixtures |
|---|---|
| Site config & admin access | SiteConfigTests, AdminEmailsTests, SiteContentRulesTests |
| BJJ landing flavor | BjjRulesTests (game plan, rank bar, principles, eras/road, now — parsing and validation) |
| Landing page (render) | LandingSectionsRenderTests, AppCssTests |
| Blog | SlugHelperTests, MarkdownServiceTests, PostRulesTests, BlogFiltersTests |
| Comments, profiles & moderation | CommentRulesTests, ProfileRulesTests, AvatarServiceTests, ReportRulesTests, BadgeLabelTests |
| Projects | ProjectRulesTests, ProjectUrlRulesTests |
| Images & uploads | ImageUploadServiceTests, OwnerPhotoServiceTests (both photo slots), FileWritesTests (the shared atomic write and best-effort delete behind the photos and the résumé) |
| Résumé | ResumeRulesTests (magic bytes, origin allowlist, size formatting), ResumeServiceTests (temp-directory write-through), ResumeLinksTests (footer/Contact link gating and admin block copy, scanned from linked Razor sources) |
| List views (paging & sorting) | PagedResultTests, PagerWindowTests, QuerySortTests, SortStateTests, SortDefaultsTests |
| UI plumbing | JsModuleUrlTests (asset-path module import), IconKindTests, NoInlineOnClickTests (no inline `onclick=""` outside the site.js `data-action` pattern) |
| Contact & spam defense | ContactRateLimiterTests, ContactSpamRulesTests, ContactFormTimestampTests, DisposableEmailDomainsTests, MailDomainCheckerTests, EmailTemplatesTests |
| Analytics | AnalyticsRulesTests (includes the admin-session Named Event exclusion and the Period day count and per-day average behind the Daily visitors tile), AnalyticsServiceTests (the shared event entry point skips admin sessions before any database access), AnalyticsRollupTests, VisitorKeyTests |
| Admin stats chart | VisitorsChartRulesTests (nice-max scale, gridline ticks, point coordinates, line/today-segment paths, date-label spread, tooltip anchor side — all pure), VisitorsChartRenderTests (HtmlRenderer render of VisitorsChart with fixed points: heading/caption, the empty state, day-group and table-row counts, the today row and dashed segment, per-day aria-labels, no script/style/external URL) |
| SEO | SeoRulesTests |
| Theming | ThemeRulesTests, ThemeToggleTooltipTests (BJJ tooltip wording, scanned from the linked Razor and JS sources) |

This reconciles the table against `ls tests/Portfolio.Tests/*.cs` (still the source of
truth if the two ever drift again) — every fixture file has a row, and the total above
is copied from a `dotnet test` run against this phase's code.

## Conventions
- xUnit; deterministic time via `Microsoft.Extensions.TimeProvider.Testing`
- Pure logic lives in `Services/` static helpers or small classes so it tests without EF;
  EF-backed services (`BlogService`, `CommentService`, …) are exercised through the
  integration checks instead
- Component markup (`LandingSections`) is pinned with a real
  `Microsoft.AspNetCore.Components.Web.HtmlRenderer` render, not bUnit — see
  `LandingSectionsRenderTests` and `AppCssTests`; shared helpers live in `tests/Portfolio.Tests/Support/`
  (`LandingRenderHarness` renders the component, `CssScanner` parses `app.css` into leaf rules)
- File-scan checks read source files copied next to the test assembly rather than a
  parser package: `Portfolio.Tests.csproj` links `app.css` (for `AppCssTests`) and every
  `.razor` file under `src/Portfolio.Web/Components/` (for `NoInlineOnClickTests`) as
  `None`/`CopyToOutputDirectory` items, read back via `AppContext.BaseDirectory`
