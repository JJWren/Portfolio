# Unit 11 — Functional Design: business rules

**Written**: 2026-09-13. Numbered BR-20 onward so they extend the Unit 10 rules (BR-1 to BR-19 in `construction/unit10-bjj-landing/functional-design/business-rules.md`), which stay in force; BR-6 and BR-9 are amended below. Every rule is pure logic in `Services/` (testable without EF) or a rendering fact pinned by `LandingSectionsRenderTests` and `AppCssTests`. Owner decisions from the grilling of 2026-09-13 are cited by their question number.

## Resolution and defaults

- **BR-20 Current belt precedence and default.** The rank bar draws `EffectiveSiteContent.CurrentBelt`: the admin override when set, else the env value `SITE_CURRENT_BELT`, else black. BR-3 with a default in place of "hidden": blank never hides anything, and only the caption gates the bar (BR-6 kept, Q4).
- **BR-21 Lenient at resolve.** An unknown stored or env value resolves as unset, so the bar draws the next fallback and, at the end, black; a bad value can never take the landing page down (BR-4). The env parse at startup never throws.

## Validation at save (`SiteContentRules.Validate`)

- **BR-22 Closed set.** The draft's current belt is blank or one of `white`, `blue`, `purple`, `brown`, `black`, trimmed and case-insensitive; anything else is refused with "Current belt must be white, blue, purple, brown or black" (FR-C6). Stored lower-case.
- **BR-23 Degrees agree with the current belt's last era (BR-9 generalized, FR-C5).** When degrees are set and the road has an era on the current belt, the stripes of the last such era must equal the degrees; otherwise the save is refused with a message naming the belt, for example "Belt degrees (2) and the purple belt era's stripes (1) disagree." Effective values are used: each of the three facts (degrees, eras, current belt) comes from the draft when supplied, else from the environment, and the message names the variable for any side that fell back (`SITE_BELT_DEGREES`, `SITE_ERAS`, `SITE_CURRENT_BELT`) with the fix hint the Unit 10 message already gives. The check runs when the draft supplies at least one of the three fields, so a save that touches none of them is never blocked by an environment-only disagreement. Unset degrees, or no era on the current belt, never conflict. Lenient at resolve (BR-4): a mismatch never hides the bar.
- **BR-24 The current belt is not below the road (Q5).** When the road has eras, the effective current belt must not rank below the highest belt among them, in ladder order white, blue, purple, brown, black; otherwise the save is refused with "Current belt (blue) is below the road's highest belt (black)." A blank current belt resolves to black and never trips the rule. Same effective values, same source naming and same trigger as BR-23. Lenient at resolve: the bar still draws the stated belt.
- **Order of checks**: BR-22, the existing field checks, then BR-24, then BR-23; the first failure is the message, as everywhere in `Validate`.

## Rendering

- **BR-25 Drawing rule per belt (BR-6 amended, FR-C3).** The rank bar renders when the caption is non-empty (unchanged) and is drawn as the current belt: body and tip in that belt's `--rank-*` constant (ADR 0002), ringed with `--border`; the stripe bar in `--belt-black` with `--belt-white` stripes for white, blue, purple and brown, and `--c-red` for black, matching `.belt-band.black .bar` on the road. Degrees keep their 0 to 6 range on every belt, as the road's eras do (FR-C4). After this unit no public component emits a `style` attribute (FR-C7): the belt reaches CSS through `data-belt` on the figure. No new animation; the reduced-motion block is unchanged.
- **BR-18 reminder.** Examples in code, tests, fixtures, `.env.example` and README use invented belts and gyms (NFR-13).
