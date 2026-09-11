# Unit Test Instructions

```bash
dotnet test                          # all tests
dotnet test --filter SlugHelperTests # one fixture
```

## Coverage (744 tests, 41 fixtures — as of Unit 13, the admin résumé upload)
| Area | Fixtures |
|---|---|
| Site config & admin access | SiteConfigTests, AdminEmailsTests, SiteContentRulesTests |
| BJJ landing flavor | BjjRulesTests (game plan, rank bar, principles, eras/road, now — parsing and validation) |
| Landing page (render) | LandingSectionsRenderTests, AppCssTests |
| Blog | SlugHelperTests, MarkdownServiceTests, PostRulesTests, BlogFiltersTests |
| Comments, profiles & moderation | CommentRulesTests, ProfileRulesTests, AvatarServiceTests, ReportRulesTests, BadgeLabelTests |
| Projects | ProjectRulesTests, ProjectUrlRulesTests |
| Images & uploads | ImageUploadServiceTests, OwnerPhotoServiceTests (both photo slots) |
| Résumé | ResumeRulesTests (magic bytes, origin allowlist, size formatting), ResumeServiceTests (temp-directory write-through), ResumeLinksTests (footer/Contact link gating and admin block copy, scanned from linked Razor sources) |
| List views (paging & sorting) | PagedResultTests, PagerWindowTests, QuerySortTests, SortStateTests, SortDefaultsTests |
| UI plumbing | JsModuleUrlTests (asset-path module import), IconKindTests, NoInlineOnClickTests (no inline `onclick=""` outside the site.js `data-action` pattern) |
| Contact & spam defense | ContactRateLimiterTests, ContactSpamRulesTests, ContactFormTimestampTests, DisposableEmailDomainsTests, MailDomainCheckerTests, EmailTemplatesTests |
| Analytics | AnalyticsRulesTests (includes the admin-session Named Event exclusion), AnalyticsRollupTests, VisitorKeyTests |
| SEO | SeoRulesTests |
| Theming | ThemeRulesTests |

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
