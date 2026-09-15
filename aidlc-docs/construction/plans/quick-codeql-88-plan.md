# Quick change plan: CodeQL issue #88 (log forging)

**Owner decision** (2026-09-15, "work it"): fix the real CodeQL finding and dismiss the false positive. **Issue**: https://github.com/JJWren/Portfolio/issues/88 (two alerts from the first CodeQL scan of 2026-08-28). **Branch** `fix/log-forging` in a separate worktree while phase 12b runs in the main tree; **PR title** `fix: keep user-supplied values from forging log lines`. **Execution**: orchestrator-implemented with the five-area review, Copilot gate, squash-merge.

## Findings

- **Alert 2, `cs/log-forging`, medium**, `Services/AnalyticsService.cs:96`: the request path is logged verbatim when a page-view insert fails. The framework hands the path percent-decoded, so an encoded line break forges a second line in the plain-text console log. Real, low impact (only `docker logs` reads it). The same shape exists once more: `Services/MailDomainChecker.cs:102` logs the sender's mail domain at Debug level.
- **Alert 1, `js/xss-through-dom`, high**, `wwwroot/js/crop.js:323`: a `blob:` object URL of the admin's own picked file assigned to the `src` of the crop stage's `<img>` (fetched by id, so the analysis cannot see the element type). An image never executes script and the upload bytes are validated server-side. Dismissed 2026-09-15 as a false positive with that rationale on the alert.

## Steps

- [x] Worktree `C:/Users/joshu/source/repos/Portfolio-codeql` on `fix/log-forging` from `origin/master` (65a7728).
- [ ] `Services/LogSafe.cs`: `Value(string? value, int maxLength = 300)` replaces `\r` and `\n` (the sanitizer shape CodeQL recognizes) and every other control character with an underscore and caps the length; empty for null.
- [ ] `Services/AnalyticsService.cs:96` and `Services/MailDomainChecker.cs:102` log `LogSafe.Value(...)` instead of the raw value.
- [ ] `tests/Portfolio.Tests/LogSafeTests.cs`: a theory over plain, CRLF, LF, tab, DEL, empty and null inputs; a fact for the cap; `unit-test-instructions.md` totals and row.
- [ ] `dotnet build -warnaserror` 0 warnings; `dotnet test` green.
- [ ] Five-area review (report-only) and remediation.
- [ ] Push; PR; Copilot gate; squash-merge; realign; remove the worktree and the branch; comment and close issue #88 once CodeQL's next scan of master closes alert 2.

## Definition of done

Alert 1 dismissed with the rationale; alert 2 closed by the merged fix; issue #88 closed with a comment naming both; no other log call interpolates a user-supplied value unsanitized.

## Deviations

(none yet)
