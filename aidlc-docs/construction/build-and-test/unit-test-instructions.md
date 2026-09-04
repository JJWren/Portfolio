# Unit Test Instructions

```bash
dotnet test                          # all tests
dotnet test --filter SlugHelperTests # one fixture
```

## Coverage (445 tests, 36 fixtures — as of v1.22.0 plus Unit 10 Phase 1)
| Area | Fixtures |
|---|---|
| Site config & admin access | SiteConfigTests, AdminEmailsTests, SiteContentRulesTests |
| Blog | SlugHelperTests, MarkdownServiceTests, PostRulesTests, BlogFiltersTests |
| Comments, profiles & moderation | CommentRulesTests, ProfileRulesTests, AvatarServiceTests, ReportRulesTests, BadgeLabelTests |
| Projects | ProjectRulesTests, ProjectUrlRulesTests |
| Images & uploads | ImageUploadServiceTests |
| List views (paging & sorting) | PagedResultTests, PagerWindowTests, QuerySortTests, SortStateTests, SortDefaultsTests |
| UI plumbing | JsModuleUrlTests (asset-path module import), IconKindTests |
| Contact | ContactRateLimiterTests |
| Landing page (render) | LandingSectionsRenderTests, AppCssTests |

This table predates several fixtures added since v1.12.0 (analytics, email, SEO, theme,
owner photo, …); a full reconciliation is planned for Unit 10 Phase 5's close-out pass
rather than piecemeal here — `ls tests/Portfolio.Tests/*.cs` is the source of truth for
the complete fixture list in the meantime.

## Conventions
- xUnit; deterministic time via `Microsoft.Extensions.TimeProvider.Testing`
- Pure logic lives in `Services/` static helpers or small classes so it tests without EF;
  EF-backed services (`BlogService`, `CommentService`, …) are exercised through the
  integration checks instead
- Component markup (`LandingSections`) is pinned with a real
  `Microsoft.AspNetCore.Components.Web.HtmlRenderer` render, not bUnit — see
  `LandingSectionsRenderTests` and `AppCssTests`; shared helpers live in `tests/Portfolio.Tests/Support/`
  (`LandingRenderHarness` renders the component, `CssScanner` parses `app.css` into leaf rules)
