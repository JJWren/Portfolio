# Requirements: admin résumé upload (Unit 13)

**Written**: 2026-09-11, after the Unit 10 addendum merged (PR #101) and while Units 11 and 12 wait. **Owner and only stakeholder**: Joshua Mykitta. **Depth** (adaptive): standard; a two-round grilling session (design tree, sixteen decisions) replaced the usual clarification questions; every round and answer is in `aidlc-docs/audit.md` under 2026-09-11. **Sources**: `src/Portfolio.Web/Endpoints/AnalyticsEndpoints.cs` (`/resume`); `src/Portfolio.Web/Services/OwnerPhotoService.cs` and `Components/Admin/SiteContentEditor.razor` (the write-through upload precedent); `Components/Pages/Contact.razor` (the only résumé link today); `Components/Layout/MainLayout.razor` (the footer); `Services/AnalyticsService.cs` and `Services/AnalyticsMiddleware.cs` (event recording and the admin exclusion); `Components/Admin/Stats.razor` (event rows); `CONTEXT.md`; `README.md`; `.env.example`; the production compose file. Section 5 of `requirements.md` (NFR-1 to NFR-9) and NFR-10 to NFR-15 of `requirements-post-unit10-followups.md` still apply where they touch this unit.

## 1. The ask (verbatim)

```text
I need the ability to be able to upload a new resume from the admin dashboard.
```

Round 1 answer: "Q8 - add link in footer next to socials/email / everything else is good". Round 2 answer: "all recommended answers are good".

## 2. Intent analysis

- **"Admin dashboard"** means the admin area. The `/admin` page is a card grid that links out to editor pages, and every file upload today lives on the Site content page, so the control goes there (decision 1).
- **"Upload a new resume"** means replace the one current file. There is no version history and no second slot (decision 6).
- **The owner widened the scope once**: a résumé link in the footer next to the socials and email (decision 8), which makes the Stats question of "which link earned the click" real (decision 13).
- Everything else follows the Owner Photo model already in production: write-through to the configured path, a Remove action, and a configuration gate.

## 3. Current state (facts found before round 1)

| Fact | Where |
|---|---|
| `RESUME_FILE` names a PDF inside the container; blank means no résumé anywhere. | `Services/SiteConfig.cs`, `.env.example`, `README.md` |
| `/resume` returns 404 unless the path is set and the file exists; otherwise it records the `resume-download` Named Event with a null target and streams the file as `application/pdf` with `Content-Disposition: attachment` and the configured path's filename. | `Endpoints/AnalyticsEndpoints.cs` |
| The only link is in the Contact page aside, rendered whenever the path is set, even when the file is missing (the click then 404s). | `Components/Pages/Contact.razor` |
| Event recording skips bots and opt-out headers but not admin sessions; page views skip admin sessions. All three events (`resume-download`, `project-click`, `contact-submit`) go through the same entry point. | `Services/AnalyticsService.cs`, `Services/AnalyticsMiddleware.cs`, `Components/Pages/Contact.razor` |
| The Stats page prints an event row as "name · target" when a target exists. | `Components/Admin/Stats.razor` |
| The footer's Social nav holds GitHub, LinkedIn, Email and, when set, the sponsor link. | `Components/Layout/MainLayout.razor` |
| The Owner Photo upload writes over the configured path through a per-call temp file and a move; its control renders only when the env path is set; Remove deletes the file. | `Services/OwnerPhotoService.cs`, `Components/Admin/SiteContentEditor.razor` |
| Production mounted the résumé folder read-only and the photo folder read-write. The live file is 16 KB. | production compose file (edited 2026-09-11, see section 8) |
| `CONTEXT.md` had no term for the résumé. | `CONTEXT.md` (term added 2026-09-11) |

## 4. Unit of work

| Unit | Title | Commit type | Size | Depends on |
|---|---|---|---|---|
| Unit 13 | Admin résumé upload | `feat:` | S, one PR, no migration | nothing; runs next, ahead of Units 11 and 12 (owner, decision 10) |

Runs under the orchestration model recorded on 2026-09-04 (one phase agent, five review agents, remediation, PR, Copilot gate, squash-merge). Unit 12 later wraps `/resume` in a rate-limit policy and does not otherwise touch it.

## 5. Functional requirements

| Id | Requirement |
|---|---|
| FR-R1 | A Résumé block on `/admin/site`, placed after the two photo blocks, with the label "Résumé" and the hint "the PDF served at /resume and linked from the footer and Contact page". The dashboard's Site content card reads "Override the landing-page copy and swap the photos and résumé." |
| FR-R2 | Configuration gate, as for the photos: with `RESUME_FILE` blank the block shows only "Set RESUME_FILE in .env (and mount its folder read-write) to enable the résumé." With it set and no file present the block shows "No résumé yet. Upload a PDF here, or copy one onto the RESUME_FILE path." plus the upload control. |
| FR-R3 | Upload accepts PDF only: the file input's accept list names PDF, and the server verifies the `%PDF-` magic bytes at the start of the stream regardless of extension or declared type. The cap is 5 MB, the same figure as the photo and image caps, as its own constant. Errors are "That file isn't a PDF." and "That file is over 5 MB." Buttons read "Upload PDF" (busy: "Uploading…") and "Remove". |
| FR-R4 | Write-through: a valid upload is written to the `RESUME_FILE` path atomically (per-call temp name in the same directory, then a move with overwrite), creating the directory when missing. A failed or oversized upload never touches the current file. After a successful replace the previous file is gone; there is no history. |
| FR-R5 | Remove deletes the file at the path (best effort, like the photo). Afterwards `/resume` returns 404 and no résumé link renders anywhere. |
| FR-R6 | One availability rule, "the path is set and the file exists", shared by the `/resume` endpoint, the Contact aside link and the footer link. The Contact link therefore stops rendering when the file is missing (today's mismatch fixed). |
| FR-R7 | With a file present the block shows the configured path's filename, the size in KB or MB, and the last-updated date from the file's write time, in the form "name · 16 KB · updated 14 Aug 2026", plus a "Download current" link to `/resume` that downloads the same bytes visitors get (no enhanced navigation on that link). |
| FR-R8 | The download filename stays the configured path's filename. The uploaded file's own name is discarded, as with the photos. |
| FR-R9 | Footer link: "Résumé" with the file icon in the footer nav after Email and before the sponsor link, rendered only when the résumé is available (FR-R6), with no enhanced navigation like the Contact link. The nav's accessible name changes from "Social" to "Links". |
| FR-R10 | The Contact link and the footer link carry a query value naming their origin (`contact`, `footer`). The endpoint records the `resume-download` event with that value as the target only when it is one of those two; any other or missing value records a null target, so Stats shows "resume-download · footer" and "resume-download · contact" rows while older rows stay as the plain row. The page-view exclusion of `/resume` is unchanged. |
| FR-R11 | Admin sessions are excluded from Named Event recording at the shared entry point, using the same role check the page-view middleware uses. This covers `resume-download`, `project-click` and `contact-submit` alike (accepted consequence: the owner's own test submissions stop counting). |
| FR-R12 | Docs: the README row for `RESUME_FILE` and the `.env.example` comment say to mount the folder read-write so the admin site-content page can replace the file, matching the photo wording. The repository compose file is unchanged (it carries no résumé mount today; the README explains the bind mount). |
| FR-R13 | Tests: pure-logic tests for the magic-byte check, the size cap and the origin allowlist; rendered tests where the existing HtmlRenderer harness reaches (the footer link and the Contact link present only when available; the block's three states if the editor renders under the harness, otherwise the rules behind them); a test pinning that an admin principal records no event. |
| FR-R14 | Glossary: the Résumé term in `CONTEXT.md` (written 2026-09-11) is the canonical language; code and copy use "résumé" in visitor-facing text and `Resume` in identifiers, never CV. |

## 6. Non-functional requirements (in addition to NFR-1 to NFR-9 and NFR-10 to NFR-15)

| Id | Requirement |
|---|---|
| NFR-16 | No new NuGet package. The magic-byte check is a span comparison; no PDF parsing. |
| NFR-17 | The endpoint keeps serving the file as an attachment with the PDF content type; nothing renders the PDF inline, so no script inside a PDF ever runs in the site's origin. |
| NFR-18 | Authorization is the existing Admin role on the page; the endpoint stays public. No new route is added. |
| NFR-19 | BR-18 holds: no owner fact in code, tests, `.env.example` or README examples. Test fixtures use invented filenames. |
| NFR-20 | `dotnet build -warnaserror` with 0 warnings; `dotnet test` green (705 tests today) plus the new tests; one PR sized for the Copilot gate with a title from the CI allow-list. |
| NFR-21 | The plain landing page's HTML changes only in the shared footer, and only when a résumé is available; with no résumé the markup of every page is byte-for-byte unchanged except the footer nav's accessible name. |

## 7. Decisions from the grilling session (the settled design tree)

| # | Decision | Chosen |
|---|---|---|
| 1 | Where the control lives | A Résumé block on `/admin/site` after the photo blocks; the dashboard card mentions it |
| 2 | Where the bytes go | Write-through over the `RESUME_FILE` path, the photo model; control only when the path is set |
| 3 | Type and cap | PDF only by magic bytes; 5 MB |
| 4 | Remove | Yes; deletes the file |
| 5 | Link follows the file | Yes; one availability rule for endpoint and links |
| 6 | History | None; single slot, atomic replace |
| 7 | What the admin sees | Filename, size, last-updated, "Download current"; admin sessions skip event counts |
| 8 | Scope | Owner override: also a footer link next to the socials and email |
| 9 | Glossary term | "Résumé"; avoid CV, curriculum vitae, resume file |
| 10 | Queue position | Next, ahead of Units 11 and 12 |
| 11 | Download filename | The configured path's filename; the upload's name is discarded |
| 12 | Footer placement | After Email, before the sponsor link; nav label "Links" |
| 13 | Stats | Event target `footer` or `contact` from an allowlisted query value |
| 14 | Admin link | Downloads the current PDF through the same route |
| 15 | Block copy | As proposed in FR-R1, FR-R2, FR-R3 and FR-R7 |
| 16 | Production compose | Edited by the session on 2026-09-11 (section 8) |

No ADR: the storage choice follows the photo precedent already in the code, and the analytics change is small and reversible.

## 8. Owner actions

- **Done 2026-09-11 by the session**: the production compose file's résumé bind mount lost its read-only flag and its comment now says the admin page can replace the file in place. It takes effect when the container is next recreated, which the release deploy does anyway. No `.env` change is needed; `RESUME_FILE` is already set.
- **After the release deploy**: open `/admin/site`, confirm the block shows the current file, upload once, and check the footer link on the public site.

## 9. Out of scope (recorded)

- Version history or a restore action; DOCX or any second format; an inline PDF viewer; storing the file in Postgres or the uploads volume; a résumé link in the hero or the top nav; rate limiting of `/resume` (Unit 12).

## 10. Approval

Pending. "Requirements analysis complete. Do you want to request changes or continue to the next stage?"

**Approval (owner, 2026-09-11)**: "A", continue to Workflow Planning with the requirements as written.
