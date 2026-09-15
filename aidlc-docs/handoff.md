# Handoff: after Unit 12 phase 12a (refreshed 2026-09-15)

**For**: a new or compacted session picking up the Portfolio repository (`C:\Users\joshu\source\repos\Portfolio`, Blazor .NET 10 + PostgreSQL, Docker Compose; owner and only stakeholder Joshua Mykitta). **State on hand-off**: master at `65a7728` (release 1.30.0, published 2026-09-15T17:57Z with its image in the registry); production running **v1.29.0** on this machine's Docker Desktop, healthy; **1.30.0 is not deployed yet**. **Next work**: the report-only deploy of 1.30.0 with the owner's admin-page check, then Unit 12 phase 12b (rate limiting), then the CodeQL issue #88 follow-up if the owner wants it fixed.

## 1. Read first, in this order

1. `aidlc-docs/aidlc-state.md`: the live status of every unit (Units 11, 13, 14 and the quick fix COMPLETE; Unit 12 phase 12a COMPLETE, phase 12b open).
2. `aidlc-docs/inception/requirements/requirements-post-unit10-followups.md`: section 4.3 (FR-D9 to FR-D16 for 12b) and **section 10** (the grilling amendments of 2026-09-13 that override earlier text: process, the stats tile, Unit 11, 12a, 12b, the production values, the survey facts).
3. `aidlc-docs/construction/plans/unit12-security-plan.md`: the Phase 12a steps (all ticked, with the verification record and a Deviations section), the Phase 12b steps (open), the release step for 12a (open: a stop before the deploy).
4. `aidlc-docs/construction/plans/unit12b-brief.md`: the self-contained brief for the 12b phase agent (branch `feat/rate-limiting`, PR title `feat: rate limiting for auth, feeds, comments and reports`), ready to launch after the branch and its `docs:` commit exist.
5. `aidlc-docs/construction/unit12-security/` (NFR requirements, tech-stack decisions, NFR design patterns and logical components) and `aidlc-docs/inception/application-design/unit12-security-components.md`: the design both phases run under; `docs/adr/0003-security-headers-are-emitted-by-the-application.md`.
6. `aidlc-docs/construction/build-and-test/security-verification-instructions.md`: the curl checks, the browser CSP walk, and the owner's production admin-page checklist under `report-only`.
7. `CONTEXT.md` (Current Belt, Rank Bar and Degree were generalized on 2026-09-13), `CONTRIBUTING.md`, `aidlc-docs/audit.md` from 2026-09-13 onward (the grilling, the quick fix, Unit 11, 12a, the deploys), and the project memory under `~/.claude/projects/C--Users-joshu-source-repos-Portfolio/memory/`.

## 2. What happened since the previous handoff (2026-09-13 to 2026-09-15)

| Item | Outcome |
|---|---|
| Grilling of the handoff (2026-09-13) | Fourteen decisions, all as recommended ("good answers"), recorded as requirements section 10 and the workflow plan's grilling outcome. Gates collapse to one approval per unit, then run to merge; stops stay before each deploy and for the owner's admin-page check. |
| Quick fix, the Daily visitors tile | PR #107 (`e79bf49`): the tile shows the average per day with the visitor-days and day count beneath. |
| Unit 11, current belt for the rank bar | PR #109 (`069ef12`): `SiteContent.CurrentBelt`, `SITE_CURRENT_BELT`, the admin select, CSS per belt through `data-belt`, the generalized degrees check (BR-23) and the not-below-the-road check (BR-24), migration `AddCurrentBelt`. Released as 1.29.0 and deployed 2026-09-15 on the owner's "deploy". |
| Unit 12 phase 12a, security headers | PR #110 (`1153ece`): the composer `SecurityHeadersRules`, `SecurityOptions` (`SECURITY_CSP_MODE`), `SecurityHeadersMiddleware` (OnStarting, fill-if-absent, a cached list per theme hash, the snapshot stored on the request), the theme snapshot hash, the uploads policy, Kestrel's server header off, the framework's own `SAMEORIGIN` and `frame-ancestors 'self'` defaults suppressed (they were the headers seen in production and had been attributed to the proxy), `<ImportMap />` removed, the theme editor painting through the CSSOM with a forced reload after save and reset, ADR 0003, README and env docs, the verification instructions. 990 tests, 54 fixtures. Released as 1.30.0 by the owner's merge of PR #111. |
| CodeQL issue #88 | Reviewed 2026-09-15 (see section 5). |

## 3. Repository state to be aware of

- Uncommitted aidlc-docs edits sit on master by convention (`aidlc-state.md`, `audit.md`, `construction/plans/unit12-security-plan.md`, this file); the next branch's first commit is a `docs:` commit folding them (stash-first realign, `git switch -c`, `git add` by explicit path). Never `git add -A`.
- Tests: 990, 54 fixtures, `dotnet build -warnaserror` at 0 warnings on `1153ece`/`65a7728`.
- The production folder `Z:\docker\portfolio` pins `ghcr.io/jjwren/portfolio:1.29.0`; the `.env` there does not yet carry `SECURITY_CSP_MODE` (blank means `enforce` in the code, so the first 1.30.0 deploy must add `SECURITY_CSP_MODE=report-only` before the recreate, per the owner's decision Q7).
- The auto-mode classifier refuses a combined tag-bump, pull and recreate as a production deploy unless the owner has just said so explicitly; on "deploy" the same chain ran.

## 4. Next steps in order

1. **Deploy 1.30.0 with report-only** (a stop the owner answers): in the production folder, append `SECURITY_CSP_MODE=report-only` to `.env` (CRLF, values masked, never `cat`), bump the compose tag to 1.30.0, `docker compose pull web`, `docker compose up -d web`, then `curl -sI https://joshuamykitta.dev/` and check the header set (`Content-Security-Policy-Report-Only`, `X-Frame-Options: DENY`, no `Server: Kestrel`; note the proxy may still print its own `Server`). Then the owner runs the admin-page checklist of `security-verification-instructions.md` with the browser console open; on the owner's "done", set `SECURITY_CSP_MODE=enforce`, recreate, curl again.
2. **Unit 12 phase 12b**: realign master stash-first, `git switch -c feat/rate-limiting`, the `docs:` commit (state, audit, the plan, this file), launch a Sonnet phase agent with `unit12b-brief.md`, five reviews, remediation, PR 4, the Copilot gate, squash-merge, then the release and the deploy with `TRUSTED_PROXIES=172.22.0.0/16` and `WEB_BIND=127.0.0.1` in the production `.env` and the compose ports line `"${WEB_BIND:-0.0.0.0}:${WEB_PORT:-8080}:8080"` (the production compose is a separate hand-maintained file). Then Build and Test (the instructions and the summary) and the Operations note.
3. **Owner checks still open**: the stats tile and the current-belt select (1.29.0), the admin pages under report-only (1.30.0).

## 5. CodeQL issue #88 (reviewed 2026-09-15)

Two open alerts in the Security tab. **Alert 2, `cs/log-forging`, medium**, at `Services/AnalyticsService.cs:96`: `logger.LogWarning(ex, "Failed to record page view for {Path}.", path)` logs the request path verbatim; the framework hands the path percent-decoded, so an encoded line break in a request can forge a line in the plain-text console log. Real but low impact (the only reader is `docker logs`). Fix: a pure `AnalyticsRules.SanitizeForLog` that strips control characters and truncates, used at that call site, with theory rows. **Alert 1, `js/xss-through-dom`, high**, at `wwwroot/js/crop.js:323`: `image.src = current.url` where the value is a `blob:` URL from `URL.createObjectURL(file)` for the admin's own picked file, assigned to an `<img>` element. An image element never executes script, a blob URL carries no attacker markup, and the uploaded bytes are validated server-side; a false positive to dismiss with that rationale (or restructure if the owner prefers a code change). Neither is fixed yet; the owner decides.

## 6. Execution model and mechanics that still matter

- **Orchestration** (owner instruction 2026-09-04): the main session orchestrates; one fresh Sonnet general-purpose phase agent per phase from a self-contained brief; five report-only review agents (correctness, security with the BR-18 grep, framework and accessibility, maintainability, performance); a remediation agent for a list, the orchestrator for one-liners; then PR, Copilot gate, squash-merge. Agents commit by explicit path, never push, never touch `audit.md` or `aidlc-state.md`.
- **Copilot**: request with `env -u GITHUB_TOKEN gh api -X POST repos/JJWren/Portfolio/pulls/N/requested_reviewers -f "reviewers[]=copilot-pull-request-reviewer[bot]"`; the reviewers list comes back empty; poll the reviews for the bot login; a review with zero new inline comments is a clean pass; re-request after a review lands, not during.
- **Git**: `env -u GITHUB_TOKEN gh ...`; stash-first realign; GitHub auto-deletes merged head branches.
- **Bash on this machine**: doubled backslashes collapse even in single quotes, so code goes through Write and Edit, never heredocs, sed or perl; new docs get `unix2dos`; `dotnet test` gates on `grep -q "Failed:     0,"`.
- **Production**: `Z:\docker\portfolio` (`/z/docker/portfolio` in Git Bash), compose LF with the tag bumped by hand, `.env` CRLF with secrets (mask values), `docker ps -a --filter name=portfolio`; the proxy is nginx-proxy-manager on the docker network `proxy` (172.22.0.0/16); database facts through `docker exec portfolio-db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f -'` with the SQL on stdin, aggregates only.
- **Owner facts** (BR-18) never appear in code, tests, fixtures, `.env.example` or README examples.

## 7. Definition of "picked up correctly"

The new session has logged its first prompt in `audit.md`, realigned master, and either performed the report-only deploy of 1.30.0 on the owner's word or started phase 12b on `feat/rate-limiting` with a `docs:` commit folding the pending edits (including this file).
