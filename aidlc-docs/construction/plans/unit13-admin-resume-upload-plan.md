# Unit 13 plan: admin résumé upload and footer link (PR A)

**Written**: 2026-09-11. **Owner request (verbatim)**: "I need the ability to be able to upload a new resume from the admin dashboard." plus the round answers "Q8 - add link in footer next to socials/email / everything else is good" and "all recommended answers are good". **Requirements**: `inception/requirements/requirements-admin-resume-upload.md` (FR-R1 to FR-R14, NFR-16 to NFR-21; approved 2026-09-11). **Workflow slot**: PR A of `inception/plans/workflow-planning-units-13-14.md`, ahead of Units 11 and 12. **Glossary**: Résumé, Named Event (`CONTEXT.md`).

**Execution note**: the workflow plan's per-PR cycle applies: one fresh Sonnet phase agent briefed from this plan, five review agents, remediation, the Copilot gate, squash-merge. The Components section below is the unit's Application Design (minimal), approved with this plan.

## Non-negotiables

- Write-through over the `RESUME_FILE` path, exactly the photo model: no new storage, no migration, no new route, no new package.
- PDF decided by magic bytes, never by extension or declared type; the file keeps being served as an attachment with the PDF content type.
- One availability rule (path set and file exists) shared by the endpoint, the Contact link and the footer link.
- Admin sessions record no Named Event, using the same role check the page-view middleware uses.
- No inline handler (the `NoInlineOnClickTests` pin stays green), no script change, no animation, no owner fact in code, tests, fixtures, `.env.example` or README examples (BR-18): fixtures use invented filenames.

## Components (Application Design, minimal)

| Component | Kind | Members and behaviour | Depends on |
|---|---|---|---|
| `Services/ResumeRules.cs` (new) | static, pure | `MaxBytes` (5 MB); `IsPdf(ReadOnlySpan<byte> header)` true when the bytes start with `%PDF-`; `ParseOrigin(string? value)` returns `"footer"` or `"contact"` for an exact match and null otherwise; `OriginQueryKey` (`from`), `FooterOrigin`, `ContactOrigin`; `FormatSize(long bytes)` giving "16 KB" or "1.2 MB" | nothing |
| `Services/ResumeService.cs` (new, singleton) | service | `IsConfigured` (path set); `IsAvailable` (path set and the file exists); `GetInfo()` returning `ResumeInfo(FileName, Bytes, LastWriteUtc)` or null; `SaveAsync(Stream, CancellationToken)` buffers with `ImageGuards.BufferWithLimitAsync` at `MaxBytes`, checks `IsPdf` on the leading bytes (throws `InvalidDataException` otherwise), creates the directory, writes a per-call temp file beside the target and moves it over the target with overwrite, deleting a stranded temp file; `Delete()` best effort like the photo; unconfigured save throws naming `RESUME_FILE` | `SiteConfig.ResumeFile`, `ImageGuards` |
| `Services/AnalyticsRules.cs` (changed) | static, pure | `IsExcludedUser(ClaimsPrincipal user)` true for the Admin role; the middleware's `ShouldConsider` and `AnalyticsService.TryRecordEventAsync` both call it | `AuthEndpoints.AdminRole` |
| `Services/AnalyticsService.cs` (changed) | service | `TryRecordEventAsync` returns early for an excluded user, next to the bot and opt-out checks | `AnalyticsRules` |
| `Endpoints/AnalyticsEndpoints.cs` (changed) | minimal API | `/resume` returns 404 unless `ResumeService.IsAvailable`; records `resume-download` with `ResumeRules.ParseOrigin` of the `from` query value as the target; serves the file as today | `ResumeService`, `ResumeRules` |
| `Components/Pages/Contact.razor` (changed) | page | the Résumé link renders only when `Resume.IsAvailable`, href `/resume?from=contact`, still no enhanced navigation | `ResumeService` |
| `Components/Layout/MainLayout.razor` (changed) | layout | footer nav `aria-label="Links"`; a Résumé anchor with the file icon after the Email anchor and before the sponsor block, href `/resume?from=footer`, `data-enhance-nav="false"`, rendered only when available; one `File.Exists` per render, as the photo already costs | `ResumeService` |
| `Components/Admin/SiteContentEditor.razor` (changed) | admin page | a Résumé block after the mat-photo alt field: the not-configured hint, the no-file hint, the info line (`name · size · updated d MMM yyyy`) with the "Download current" link (`/resume`, no enhanced navigation), the "Upload PDF" label wrapping an `InputFile` with `accept="application/pdf,.pdf"`, the "Remove" button; `UploadResumeAsync` checks `e.File.Size` against `MaxBytes` first ("That file is over 5 MB."), then opens the stream at `MaxBytes` and calls `SaveAsync`, mapping `InvalidDataException` to "That file isn't a PDF." and any other `IOException` to its own message (a read-only mount shows as the write error); `RemoveResume`; `_resumeInfo`, `_resumeBusy` | `ResumeService`, `ResumeRules` |
| `Components/Admin/Dashboard.razor` (changed) | admin page | Site content card text "Override the landing-page copy and swap the photos and résumé." | nothing |
| `Program.cs` (changed) | composition | registers `ResumeService` as a singleton next to `OwnerPhotoService` | |

`SiteConfig` is unchanged: `ResumeFile` already exists. No CSS is expected: the block reuses the photo block's `field`, `editor-actions`, `btn btn-ghost photo-upload` and `muted` classes; if the info line needs a class, it goes in the admin stats section of `app.css` as a one-liner.

## What the code looks like today (evidence)

- `/resume` (`Endpoints/AnalyticsEndpoints.cs`) checks `site.ResumeFile is null || !File.Exists(...)`, records the event with a null target, and returns `Results.File(path, "application/pdf", fileDownloadName: Path.GetFileName(path))`.
- `Contact.razor` renders the link on `Site.ResumeFile is not null` (the missing-file mismatch); the footer (`MainLayout.razor`) has GitHub, LinkedIn, Email, then the sponsor link, in a nav labelled "Social".
- `TryRecordEventAsync` skips bots and opt-out headers only; `AnalyticsMiddleware.ShouldConsider` also skips `context.User.IsInRole(AuthEndpoints.AdminRole)`. `Contact.razor` records `contact-submit` through the same call with `HttpContext`.
- `OwnerPhotoService.SaveAsync` is the atomic-write template (per-call temp name, `File.Move(temp, target, overwrite: true)`, stranded temp cleanup); `ImageGuards.BufferWithLimitAsync` is the size guard; `SiteContentEditor.razor` lines 111 to 205 are the block template, with `_error` shown above the form.
- Razor sources are linked into the test output under `RazorComponents/` and pinned by text scans (`ThemeToggleTooltipTests`, `NoInlineOnClickTests`); `OwnerPhotoServiceTests` is the temp-directory service test template; `unit-test-instructions.md` still says 697 tests and 38 fixtures (705 tests after PR #101).
- No development sign-in exists, so the admin pages cannot be driven locally; earlier units covered them with tests and the owner's production check.

## Steps

- [x] Branch `feat/admin-resume-upload` from a realigned master (stash-first); first commit `docs:` folding every pending aidlc-docs edit (audit, state, both requirements documents, both workflow plans, this plan, `CONTEXT.md`).
- [x] `Services/ResumeRules.cs`: the pure members above, with XML summaries in the repository style.
- [x] `Services/ResumeService.cs`: the service above; `Program.cs` registration.
- [x] `Services/AnalyticsRules.cs` and `Services/AnalyticsMiddleware.cs`: `IsExcludedUser` extracted and used; `Services/AnalyticsService.cs`: `TryRecordEventAsync` returns early for an excluded user.
- [x] `Endpoints/AnalyticsEndpoints.cs`: availability through the service, the origin target, the comment updated (no longer "config-gated" alone).
- [x] `Components/Pages/Contact.razor` and `Components/Layout/MainLayout.razor`: the links as specified; the nav label "Links".
- [x] `Components/Admin/SiteContentEditor.razor`: the Résumé block and handlers; `Components/Admin/Dashboard.razor`: the card text.
- [x] `README.md` (the `RESUME_FILE` row: mount its folder read-write so the admin site-content page can replace it) and `.env.example` (the same sentence in the comment), matching the photo wording.
- [x] Tests: `ResumeRulesTests` (IsPdf true for a `%PDF-1.7` header and false for short or other bytes; ParseOrigin exact matches only, null for other, empty, or differently cased values; FormatSize for bytes, KB and MB); `ResumeServiceTests` on a per-test temp directory (unconfigured and missing-file availability; GetInfo name, size and write time; SaveAsync writes the exact bytes over the path and creates the directory; a non-PDF is rejected and the existing file is untouched; an oversized stream is rejected; no temp file remains; Delete removes; Delete unconfigured does not throw; unconfigured save names `RESUME_FILE`); `AnalyticsRulesTests` (IsExcludedUser true for a principal with the Admin role claim, false for anonymous and for a plain user); razor pins in a new `ResumeLinksTests` on the linked sources (MainLayout: the nav label, the Résumé anchor with `?from=footer` and `data-enhance-nav="false"` positioned after the Email anchor and before the sponsor block, gated by the availability member; Contact: the anchor with `?from=contact` gated by the availability member; SiteContentEditor: the three copy strings, the accept attribute, the Download current link; Dashboard: the card text).
- [x] `construction/build-and-test/unit-test-instructions.md`: the count line and the fixtures table refreshed (two new fixtures) — actually three new fixture files (`ResumeRulesTests`, `ResumeServiceTests`, `ResumeLinksTests`); `AnalyticsRulesTests` was extended, not added.
- [x] `dotnet build -warnaserror` with 0 warnings; `dotnet test` green; the summary line checked by the gate (`Failed:     0,`).
- [x] Local check with an ephemeral, isolated Postgres on a throwaway port and a temporary folder as `RESUME_FILE`: with a PDF present, `/contact` and the footer show the link and `/resume?from=footer` downloads with the configured name; the `AnalyticsEvents` table gains rows with targets `footer` and `contact` (queried in the throwaway database); with the file removed, both links vanish and `/resume` returns 404; `/resume?from=other` records a null target. The admin block cannot be driven locally (no development sign-in); its states are covered by the pins and the service tests, and the owner's check after the release deploy (requirements section 8) closes the loop.
- [x] Five-area review (report-only: correctness, security including the BR-18 grep, framework, maintainability, performance) and remediation; build and tests green after every applied finding. Done 2026-09-11: security PASS (BR-18 grep clean); correctness one major applied (UnauthorizedAccessException added to the résumé and both photo upload catch filters); framework PASS with three observations, none needing a change; maintainability major applied (ThemeToggleTooltipTests row, 42 fixtures) and minors applied (TooLargeMessage derived from MaxMegabytes with a test; CSS comment scope), nit deferred to a follow-up task (shared atomic-write helper); performance minor declined (the per-render stat stays uncached, reasoning in the IsAvailable summary) and nit applied (CopyToAsync into the temp FileStream). Build 0 warnings, 745 tests.
- [ ] Push; PR `feat: admin resume upload and footer link`; Copilot gate per CONTRIBUTING.md; squash-merge; realign master stash-first; delete the branch; tick this plan, the state file, the audit and memory.
- [ ] After the merge: release-please proposes the next minor; the owner bumps the compose image tag and recreates the container (which also activates the read-write résumé mount edited 2026-09-11); the owner's check per requirements section 8.

## Definition of done

The admin can replace or remove the résumé from the Site content page; the footer and Contact links exist exactly when the file does; downloads keep the configured filename and count with their origin; the admin's own clicks and submissions count nothing; build and tests green; PR merged with a clean Copilot pass.

**Approved by the owner on 2026-09-11: "A".** Part 2 started the same day.
