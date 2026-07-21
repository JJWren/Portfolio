# Unit 6 Plan — Projects (Admin CRUD + Carousel)

Admin-curated project cards shown on a horizontal scroll-snap carousel (CSS + a few lines of JS,
no framework interactivity on the public page). Cards: header image (ribbon-gradient fallback),
title, summary, homepage | repo links. Page links out to the GitHub landing page from GITHUB_URL.

- [x] Project entity (title, summary ≤500, header image, homepage/repo URLs, SortOrder, IsVisible,
      timestamps) + AddProjects migration
- [x] ProjectService (visible ordered by SortOrder, admin list, save w/ auto sort-append, delete,
      move up/down via neighbor swap)
- [x] Public /projects: scroll-snap carousel with prev/next buttons (site.js scroll helper),
      ribbon-gradient fallback image with initial, GITHUB_URL call-to-action + intro link
- [x] Admin /admin/projects list (order arrows, visibility badges) + /admin/projects/new|{id}/edit
      editor (InteractiveServer: image upload, visible toggle, two-step delete confirm)
- [x] Dashboard projects card links to admin list
- [x] Carousel + project card styles on existing tokens
- [x] Verify: 38 tests green; compose: seeded cards render with repo links, hidden project excluded,
      fallback image + GitHub CTA + carousel controls present
