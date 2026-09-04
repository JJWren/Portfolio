# Unit 10 — Functional Design: domain entities (new data fields)

**Written**: 2026-09-04. **Depth**: minimal (per the handoff). **Scope**: only what Unit 10 adds. `SiteContent`, `SiteConfig`, `EffectiveSiteContent` and `OwnerPhotoService` are extended, never replaced; the plain landing page (no flavor) keeps today's data path untouched.

Sources: `construction/plans/unit10-bjj-landing-handoff.md` (decisions 1-7), `inception/requirements/requirements.md` section 5, `inception/application-design/landing-directions/parts/Main.body.html`.

## 1. Flavor (startup configuration)

| Item | Definition |
|---|---|
| `SiteFlavor` enum | `Default`, `Bjj` |
| `SiteConfig.Flavor` | Parsed from `SITE_FLAVOR`. `bjj` (trimmed, case-insensitive) maps to `Bjj`; blank or any other value maps to `Default` (decision 1: unknown values fall back to the plain landing page). |

Flavor is startup configuration like every `SITE_*` value and is not admin-editable: it decides which markup and CSS ship, not which words.

## 2. `SiteContent` columns (single row, `Id = 1`)

| Column | Type | Limit | Env fallback | Phase | Meaning |
|---|---|---|---|---|---|
| `HeroEyebrow` | text | 120 chars | `SITE_HERO_EYEBROW` | 2 | The line above the h1 (the owner's name and title; decision 5) |
| `GamePlan` | text[] | exactly 4 lines, 500 chars each | `SITE_GAME_PLAN` | 2 | Chart nodes, one per line: `term \| reading \| how` |
| `BeltCaption` | text | 200 chars | `SITE_BELT_CAPTION` | 2 | Caption under the rank bar |
| `BeltDegrees` | int, nullable | 0 to 6 | `SITE_BELT_DEGREES` | 2 | Degree stripes drawn on the rank bar |
| `Principles` | text[] | 1 to 6 lines, 500 chars each | `SITE_PRINCIPLES` | 2 | `maxim \| reading` |
| `Eras` | text[] | 1 to 12 lines, 500 chars each | `SITE_ERAS` | 3 | `date \| belt \| stripes \| gym \| location \| role` |
| `Now` | text[] | 1 to 8 lines, 500 chars each | `SITE_NOW` | 3 | `label \| value` |
| `OwnerPhotoFlipAlt` | text | 200 chars | `OWNER_PHOTO_FLIP_ALT` | 4 | Alt text for the second (mat) portrait |

Contract, unchanged from `Tagline`, `About` and `Skills`: a null column (or an empty array) means "use the env value"; an env value that is blank means the section is hidden. There is no way to force-blank a non-empty env value from the admin page.

The h1 stays in `HeroHeading` (default: the owner name, so the plain flavor is unchanged; Joshua sets "Position before submission." at `/admin/site`). The tagline stays in `Tagline` (decision 2). About and skills are untouched.

Text[] columns map like `Skills` today (`PrimitiveCollection<List<string>>`, Postgres `text[]`, one element per line). Line and field limits are enforced in `SiteContentRules`, not by the schema.

Env naming note: requirements.md section 9 suggested `SITE_TIMELINE` for the eras fallback; this design uses `SITE_ERAS` so the env name matches the column and the glossary term. Change it at plan approval if you prefer the older name.

## 3. Typed view: `EffectiveSiteContent` additions

`SiteContentRules.Resolve` keeps returning one immutable snapshot per request; the new members are parsed once there so components never split strings.

| Member | Type | Empty means |
|---|---|---|
| `HeroEyebrow` | `string?` | no eyebrow |
| `GamePlan` | `IReadOnlyList<GamePlanNode>` | no chart |
| `BeltCaption` | `string?` | no rank bar |
| `BeltDegrees` | `int` (0 when unset) | a plain black belt |
| `Principles` | `IReadOnlyList<Principle>` | no Principles section |
| `Eras` | `IReadOnlyList<Era>` | no road (ladder and table) |
| `Now` | `IReadOnlyList<NowItem>` | no Now section |
| `OwnerPhotoFlipAlt` | `string` | resolved like `OwnerPhotoAlt` (env, then "Portrait of {owner}") |

Records (in `Services/BjjRules.cs`, pure and test-friendly):

```csharp
public enum Belt { White, Blue, Purple, Brown, Black }
public sealed record GamePlanNode(string Term, string Reading, string How);
public sealed record Principle(string Maxim, string Reading);
public sealed record Era(DateOnly Date, Belt Belt, int Stripes, string Gym, string Location, string Role);
public sealed record NowItem(string Label, string Value);
```

`Belt` is the closed set the CSS knows (`--rank-white` to `--rank-black`, ADR 0002). Anything else is rejected at save and dropped at resolve.

## 4. Second portrait slot (decision 3)

| Item | Definition |
|---|---|
| `OwnerPhotoSlot` enum | `Primary` (the desk photo, today's slot), `Flip` (the mat photo) |
| `SiteConfig.OwnerPhotoFlipFile`, `OwnerPhotoFlipAlt` | From `OWNER_PHOTO_FLIP_FILE` and `OWNER_PHOTO_FLIP_ALT`, same semantics as the primary pair |
| `OwnerPhotoService` | Every member takes a slot with `Primary` as the default, so existing callers and tests compile unchanged: `IsConfigured(slot)`, `GetVersionedUrl(slot)`, `SaveAsync(stream, slot)`, `Delete(slot)` |
| Endpoint | `/owner-photo-flip`, the same handler as `/owner-photo` bound to the flip path (sniffed content type, versioned immutable caching) |

The hero shows the two-photo switch only when both slots resolve to an existing file; with one photo it renders exactly today's single `<img class="owner-photo">`.

## 5. Line formats

- **Storage and admin textareas**: one item per line, real newlines. Fields inside a line are separated by `|`; surrounding whitespace is trimmed. No field may contain `|`.
- **Env values**: the same lines joined with the literal two characters `\n` (the `SITE_ABOUT` convention), because `.env` files cannot hold real newlines.
- `date` is `YYYY-MM-DD`. `belt` is one of `white`, `blue`, `purple`, `brown`, `black` (case-insensitive). `stripes` is `0` to `6`; blank counts as `0`.

Example (the owner's own values are in the content sheet of `construction/plans/unit10-bjj-landing-plan.md`):

```text
2020-09-23 | brown | 4 | Iron Grip BJJ | Fairhope, AL | Transitioning into tech.
```
