# Contributing

## Branch & release flow

- Work happens on feature branches; `master` only moves by squash-merged PRs.
- PR titles must be conventional commits (`feat:`, `fix:`, `chore:`, …) — the squash
  commit title feeds release-please, which maintains the release PR. Merging the
  release PR tags a version and publishes `ghcr.io/jjwren/portfolio:<version>`.

## PR review gate (required)

**No PR is merged until a reviewer pass comes back clean.**

1. Open the PR and wait for the automatic reviewer (GitHub Copilot) to complete
   its review. If it doesn't trigger, request it explicitly.
2. Read every comment. Remediate real issues in follow-up commits on the same
   branch; if a comment is a false positive, reply on the thread explaining why
   it isn't being changed.
3. Re-request review after pushing changes and repeat until a review pass
   produces **no new actionable comments**.
4. Only then squash-merge.

This applies to feature PRs and fix PRs alike (release-please's automated
release PRs are exempt — they contain only generated changelog/version bumps).

## Quality bar

- `dotnet build -warnaserror` and `dotnet test` must pass locally before pushing.
- New behavior ships with tests where the logic is unit-testable (validation
  rules, services with extractable logic).
- Schema changes always go through an EF migration; migrations apply on startup.
