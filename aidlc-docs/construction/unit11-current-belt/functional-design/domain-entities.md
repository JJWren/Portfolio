# Unit 11 — Functional Design: domain entities (the current belt)

**Written**: 2026-09-13, from the grilling session of the same day (`audit.md`, round 1, Q4 to Q6) and FR-C1 to FR-C10 of `inception/requirements/requirements-post-unit10-followups.md` with its section 10 amendments. **Depth**: minimal. **Scope**: one new field with an env fallback and a default, one typed member on the effective content, one draft field, one admin select. `SiteContent`, `SiteConfig`, `EffectiveSiteContent`, `SiteContentDraft` and `SiteContentValues` are extended the Unit 10 way; nothing is replaced. The plain landing page (no flavor) keeps today's data path untouched.

Glossary: `CONTEXT.md` terms Current Belt (new), Rank Bar and Degree (generalized), written in the grilling session.

## 1. `SiteContent` column (single row, `Id = 1`)

| Column | Type | Values | Env fallback | Meaning |
|---|---|---|---|---|
| `CurrentBelt` | text, nullable | `white`, `blue`, `purple`, `brown`, `black` (stored lower-case) | `SITE_CURRENT_BELT` | The belt the rank bar draws |

Contract as for every BJJ column: null means "use the env value". Unlike the other BJJ fields, a blank env value hides nothing: it means the default, black (FR-C2; requirements decision 1), so a deployment that never sets the field renders the rank bar exactly as v1.28.0 does. Migration `AddCurrentBelt` adds the column; there is no data migration. If `AppDbContext` sizes the short text columns from the `SiteContentRules` constants, the new column gets a constant of its own (16 is plenty for the five names).

## 2. `SiteConfig`

| Member | Type | Parsed from | Rule |
|---|---|---|---|
| `CurrentBelt` | `Belt?` | `SITE_CURRENT_BELT` | The trimmed, case-folded value parsed against the closed set; blank or unknown parses to null. Startup never throws (BR-21). |

Env name `SITE_CURRENT_BELT`, matching the column and the glossary term the way `SITE_ERAS` matches `Eras` (requirements decision 2). A trailing positional parameter with the default null, so every construction site keeps compiling.

## 3. `EffectiveSiteContent`

| Member | Type | Resolution |
|---|---|---|
| `CurrentBelt` | `Belt`, never null | override, then env, then `Belt.Black` (BR-20); an unparsable stored value counts as unset (BR-21) |

A trailing positional parameter with the default `Belt.Black`, after `OwnerPhotoFlipAlt`, so callers that predate this unit (tests, other constructors) keep compiling, as with every Unit 10 addition.

## 4. `SiteContentDraft` and `SiteContentValues`

| Record | New member | Position | Content |
|---|---|---|---|
| `SiteContentDraft` | `CurrentBeltText` (`string?`) | trailing positional; every construction site uses named arguments (FR-C8, the Phase 4 convention) | the select's value: blank, or one of the five names |
| `SiteContentValues` | `CurrentBelt` (`string?`) | trailing | the normalized lower-case name, or null for blank |

`SiteContentService.SaveAsync` assigns `row.CurrentBelt` next to `row.BeltDegrees`; the cache invalidation is unchanged.

## 5. Admin editor field (`/admin/site`, BJJ group only, BR-1)

A `<select>` between "Belt caption" and "Belt degrees" (FR-C8):

| Item | Value |
|---|---|
| Label | Current belt |
| Hint | the belt the rank bar draws |
| Blank option | "Default (black)", or "Default (from env: purple)" when `SITE_CURRENT_BELT` parses, so the admin sees what blank resolves to, the way the text inputs show the env value as their placeholder |
| Options | White, Blue, Purple, Brown, Black, values lower-case |

The "Belt degrees" hint becomes "stripes on the rank bar, 0 to 6 (degrees on a black belt)" (FR-C4); its column, range and env name do not change.

## 6. Rendering: `RankBar`

| Item | Definition |
|---|---|
| Parameter | `Belt Belt` (default `Belt.Black`), passed by `LandingSections` from `Content.CurrentBelt` |
| Markup | `data-belt="<name>"` on the `<figure class="rank-bar">`; the inline `style` attribute is gone (FR-C7) |
| CSS | `.rank-bar[data-belt='<belt>'] { --rank: var(--rank-<belt>); }` for the five belts, mirroring `.row[data-belt='<belt>']` on the road; body and tip `background: var(--rank, var(--belt-black))`; the stripe bar `--belt-black` with `--belt-white` stripes; `.rank-bar[data-belt='black'] .belt-bar { background: var(--c-red); }` so the black belt keeps its red bar, matching `.belt-band.black .bar` |
| Unchanged | the `--border` ring (the white belt reads on the light theme), the stripe count and its clamp, the `stripe-on` animation and the reduced-motion block, the caption gating the bar |

The colors stay the fixed `:root` constants of ADR 0002; no theme token is added or read.
