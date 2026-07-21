# Unit 2 Plan — Theming, Layout, Static Pages

Design tokens: bg #151515; accents red #A63D40 (graphic only on dark — fails text contrast),
gold #E9B872 (primary interactive), green #90A959 (tags/positive), blue #6494AA (secondary/info).
Light variant on warm paper #F6F3EC with darkened accent variants for contrast.
Type: Fraunces (display), Public Sans (body), JetBrains Mono (labels/code) — bundled woff2, no CDN.
Signature: four-color ribbon stripes as brand mark / section accents.

- [x] Download + bundle fonts (wwwroot/fonts, @font-face) — 3 latin variable woff2, ~125 KB total
- [x] Replace app.css with token system (dark default, [data-theme=light] overrides) + base styles
- [x] Remove Bootstrap lib + template pages (Counter, Weather); strip template sidebar layout
- [x] App.razor: pre-paint theme bootstrap script, fonts, meta; drop bootstrap link
- [x] ThemeToggle (static button in nav + wwwroot/js/theme.js, localStorage + prefers-color-scheme)
- [x] SiteConfig service bound from env, +SITE_ABOUT/SITE_SKILLS, startup validation + 4 tests
- [x] MainLayout: top nav (ribbon mark + name, Projects/Blog/Contact, toggle) + footer w/ palette strip
- [x] Home/About page: hero + ribbons signature, about section, skill chips, social links from SiteConfig
- [x] Verify: build + 5 tests green; compose serves new UI + all assets 200 (visual review by user — no browser tools this session)
