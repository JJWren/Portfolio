# Unit Test Instructions

```bash
dotnet test                          # all tests
dotnet test --filter SlugHelperTests # one fixture
```

## Coverage (1001 tests, 55 fixtures — as of the CodeQL issue #88 quick fix: LogSafe.Sanitize keeps user-supplied values (the request path, a sender's mail domain, Identity's error text) from forging log lines, pinned by LogSafeTests — on top of Unit 12a's security headers and its Copilot remediation (PR #110), Unit 11, the current belt for the rank bar, the Daily visitors tile quick fix (PR 1.5) and Unit 14, the admin stats daily-visitors chart)
| Area | Fixtures |
|---|---|
| Security | SecurityHeadersRulesTests (every header and CSP directive pinned, including the fuller 19-feature Permissions-Policy and the X-DNS-Prefetch-Control/X-Permitted-Cross-Domain-Policies pair; ParseCspMode's rows; the hash-only-when-given and no-unsafe-inline/eval/nonce guarantees; the uploads policy), SecurityHeadersMiddlewareTests (a bare HttpContext driven through the middleware with a fill-if-absent RecordingResponseFeature; all three SECURITY_CSP_MODE values; the composed header list — now an immutable ComposedHeaders record behind a volatile field — is cached by hash, the same list instance when two requests share a hash, a new one once it changes; the request item is set to the snapshot used, matching the composed CSP hash; ThemeService.LastKnown is preferred over a fresh GetSnapshotAsync, proven with a call-counting throwing IDbContextFactory), ThemeServiceTests (LastKnown is set on a successful GetSnapshotAsync load, through a virtual GetOverridesAsync seam since the project has no test host or in-memory EF provider, and survives a later database blip at its previous value), ProgramPipelineTests (a text scan of the linked Program.cs: the middleware's placement after UseForwardedHeaders and before UseRouting, the Kestrel server-header switch, the SecurityOptions registration immediately after SiteConfig with only whitespace/comments between them, the two framework anti-clickjacking suppressions, the uploads OnPrepareResponse additions), NoInlineStyleTests (no linked `.razor` file carries a real `style=` attribute, scanned next to NoInlineOnClickTests), ColorPickerModuleTests (the linked colorpicker.js exports applyDataStyles and paints through the CSSOM, never `setAttribute('style'`), AppRazorTests (the linked App.razor keeps the override `<style>` block on its own single line and carries no `<ImportMap`; declares a cascading HttpContext parameter; reads the theme snapshot from SecurityHeadersMiddleware's request item key with ThemeStore.GetSnapshotAsync as the text-pinned fallback), LogSafeTests (control characters replaced, the length capped and a half surrogate dropped before a user-supplied value reaches a log line, behind AnalyticsService, MailDomainChecker and the sign-in error log; CodeQL cs/log-forging, issue #88) |
| Site config & admin access | SiteConfigTests (includes SITE_CURRENT_BELT parsing), AdminEmailsTests, SiteContentRulesTests, SiteContentEditorTests (the current-belt select: options, the blank-option label, the binding and the field order, scanned from the linked Razor source) |
| BJJ landing flavor | BjjRulesTests (game plan, rank bar, current belt, principles, eras/road, now — parsing and validation) |
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
| Theming | ThemeRulesTests (includes OverrideCssHash/StyleHash: sha256- of the override CSS, null when nothing is overridden), ThemeToggleTooltipTests (BJJ tooltip wording, scanned from the linked Razor and JS sources) |

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
  parser package: `Portfolio.Tests.csproj` links `app.css` (for `AppCssTests`), `js/theme.js`
  and `js/colorpicker.js` (for `ThemeToggleTooltipTests` and `ColorPickerModuleTests`),
  `Program.cs` (for `ProgramPipelineTests`), and every `.razor` file under
  `src/Portfolio.Web/Components/` (for `NoInlineOnClickTests`, `NoInlineStyleTests` and
  others) as `None`/`CopyToOutputDirectory` items, read back via `AppContext.BaseDirectory`.
  `Support/LinkedSource.cs` centralizes the single-file "combine with
  `AppContext.BaseDirectory`, assert it exists, `ReadAllText`" idiom for every fixture that
  reads one linked file (`ProgramPipelineTests`, `ColorPickerModuleTests`, `AppRazorTests`,
  `ResumeLinksTests`, `ThemeToggleTooltipTests`, `SiteContentEditorTests`); the two fixtures
  that scan a whole linked directory (`NoInlineOnClickTests`, `NoInlineStyleTests`) keep
  their own `Directory.Exists`/`GetFiles` helper, a different shape it doesn't cover
