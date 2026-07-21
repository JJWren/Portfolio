# Unit Test Instructions

```bash
dotnet test                          # all tests
dotnet test --filter SlugHelperTests # one fixture
```

## Coverage (41 tests)
| Fixture | Covers |
|---|---|
| SiteConfigTests | Env binding, required-key failure listing, defaults, skills parsing, blank→null |
| AdminEmailsTests | ADMIN_EMAILS parsing, case-insensitive matching, empty/null handling |
| SlugHelperTests | URL-safe slug generation, accents, length cap |
| MarkdownServiceTests | Markdig rendering (headings, code fences, tables), reading time |
| CommentRulesTests | Body trim/required/max-length validation |
| ContactRateLimiterTests | Fixed-window limit, per-key isolation, window reset (FakeTimeProvider) |

## Conventions
- xUnit; deterministic time via `Microsoft.Extensions.TimeProvider.Testing`
- Pure logic lives in `Services/` static helpers or small classes so it tests without EF;
  EF-backed services (`BlogService`, `CommentService`, …) are exercised through the
  integration checks instead
