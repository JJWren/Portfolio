# Reverse Engineering Timestamp

| Field | Value |
|---|---|
| Executed | 2026-09-03 (UTC) |
| Repository | https://github.com/JJWren/Portfolio |
| Branch / commit | `master` @ `e1825d9` ("docs: README truth pass for v1.19–v1.22 features (#86)") |
| Release tag | `v1.22.0` (2026-08-15) |
| Working tree | clean; no uncommitted changes |
| Live site | https://joshuamykitta.dev |
| Trigger | Design request: review and document the site, then explore BJJ-themed landing-page directions on a design canvas |

## Scope

Everything under `src/Portfolio.Web` and `tests/Portfolio.Tests` (excluding `bin/` and `obj/`), the solution and tooling files (`Portfolio.slnx`, `global.json`, `dotnet-tools.json`), `docker-compose.yml`, `.env.example`, `.github/workflows/*`, `README.md`, `CONTEXT.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `docs/adr/*`, and the existing `aidlc-docs/` tree.

Out of scope: the production `.env` values (secrets; only the presence or absence of settings was noted), the deployed container host, and the old `JJWren/Personal_Portfolio` repository (archived).

## Method

Three read-only sweeps, each a separate agent pass, followed by direct verification of every cited file and line by the reviewer:

1. Design system and theming: `wwwroot/app.css` token by token, `wwwroot/js/*`, `Services/ThemeRules.cs`, `Services/ThemeService.cs`, `Data/ThemeSettings.cs`, `Components/Admin/ThemeEditor.razor`, fonts, icons, brand assets.
2. Landing page and site structure: `Components/Pages/Home.razor`, `Components/LandingSections.razor`, `Components/Layout/*`, `Components/App.razor`, every `@page` route, every service registration and middleware in `Program.cs`, the content model in `Data/*`, render modes, JS interop sites, tests, and the existing documentation.
3. Quality and gaps: engineering hygiene, security posture, accessibility, SEO, performance, product completeness for a senior .NET developer who is also a Brazilian Jiu-Jitsu black belt, and git history.

## Counters corrected by this pass

| Counter | Stale value (where) | Actual value at e1825d9 |
|---|---|---|
| Test methods | 184 tests / 23 fixtures (`aidlc-state.md`, `construction/build-and-test/unit-test-instructions.md`) | 250 `[Fact]`/`[Theory]` methods across 35 files (34 fixtures plus the `UnitTest1.cs` scaffold), 223 `[InlineData]` rows; xUnit reports about 390 cases |
| Reverse Engineering stage | "skipped (greenfield)" (`aidlc-state.md`) | executed 2026-09-03 (brownfield at v1.22.0) |
| GHCR publish trigger | "CI on pushes to master and v* tags" (`README.md`) | only `release-please.yml` `publish-image`, and only when a release is created |
| Production content | First draft of the assessment read the local `.env` as production (placeholder tagline and skills, blank `PUBLIC_BASE_URL`, no résumé, no photo) | Live checks on 2026-09-03/04: real tagline, About, skills, portrait, meta description and résumé are all live; one demo post remains. The assessment was corrected on 2026-09-04 |

## Artifacts produced

- `architecture.md`
- `code-structure.md`
- `api-documentation.md`
- `component-inventory.md`
- `technology-stack.md`
- `dependencies.md`
- `code-quality-assessment.md`
- `reverse-engineering-timestamp.md` (this file)
- `../requirements/requirements.md`
- `../application-design/landing-directions/` (design-canvas sources)
