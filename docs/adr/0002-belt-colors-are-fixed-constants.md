# Belt and rank colors are fixed :root constants, not theme tokens

The BJJ landing flavor draws a rank bar (and, in a later phase, a belt
ladder and per-row belt bands). Belt and rank colors are defined by the
sport, not by this site's brand: a black belt has to render as black in
both the dark and light theme, a blue belt as blue, and so on, or the
graphic stops reading as a belt. We add seven fixed `:root` constants
(`--belt-black`, `--belt-white`, `--rank-white`, `--rank-blue`,
`--rank-purple`, `--rank-brown`, `--rank-black`) instead of admin-editable
theme tokens, and the existing 26-token theme catalog (`/admin/theme`,
`ThemeRules.Tokens`) is untouched. The rank bar's stripe bar itself reuses
the existing brand `--c-red` token rather than a new constant, since it is
already fixed in both themes for the same reason (the ribbons, the footer
strip) and never carries text.

## Considered Options

- **Add them to the theme token catalog** — rejected: the catalog exists so
  an owner can retheme text-bearing brand colors, with contrast warnings and
  pinned tests that assume every token can carry text somewhere. A belt
  swatch is not text and must not follow the visitor's chosen palette — a
  black belt recolored to the accent hue is no longer a black belt, and it
  would add non-text noise to a catalog built around text-contrast checks.
- **Derive them from the brand palette with `color-mix`** — rejected: there
  is no formula that turns four brand colors (red, gold, green, blue) into
  five accurate belt colors. Belt colors would drift with every brand-color
  edit instead of staying the sport's actual colors.

## Consequences

- The seven constants are not admin-editable and never appear on
  `/admin/theme`; only a code change can alter them.
- They render identically in light and dark mode — the point, since belts
  don't change color with the reader's theme preference.
- The admin theme-preview frame (`Components/Admin/ThemeEditor.razor`, an
  inert half-width preview of `LandingSections`) inherits them automatically
  from `:root`, so the rank bar previews correctly with no extra wiring.
- Belt red (`--c-red`, reused for the rank bar's stripe bar) never carries
  text, so its ~2.9:1 contrast on dark never needs a text-contrast exemption.
