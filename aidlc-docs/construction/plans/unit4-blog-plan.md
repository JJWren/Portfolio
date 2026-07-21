# Unit 4 Plan — Blog

Markdown blog with drafts, tags, reading time, syntax-highlighted code blocks (Prism bundled
locally, palette-matched theme), and an interactive admin editor with live preview.

- [x] BlogPost entity (slug unique, title, summary, markdown body, header image, tags text[],
      published flag, timestamps) + AddBlogPosts migration
- [x] DbContextFactory registration (safe EF use from interactive components) alongside scoped context
- [x] Services: MarkdownService (Markdig advanced pipeline + reading time), SlugHelper,
      BlogService (public + admin queries, CRUD), ImageUploadService (uploads volume, /uploads static)
- [x] Public: /blog list (published, newest first, tags + reading time), /blog/{slug} detail
      (rendered markdown, header image, tags, date, real 404 for bad slugs)
- [x] Admin: /admin/posts table + /admin/posts/new + /admin/posts/{id}/edit — InteractiveServer editor
      with live preview, slug auto-suggest, tag input, header-image upload, publish toggle,
      two-step delete confirm; dashboard card now links to posts
- [x] Prism core + 11 languages bundled (37 KB), palette-matched token CSS,
      enhancedload re-highlight hook in site.js
- [x] Prose/blog/editor/admin-table/forms CSS on existing tokens
- [x] Tests: SlugHelper (10 cases), MarkdownService (render + reading time) — 32 total green
- [x] Verify: compose up; seeded post via psql; /blog list + reading time render; /blog/hello-world
      renders bold/code-fence/tags; missing slug → HTTP 404. (Admin editor UI needs OAuth sign-in
      to exercise live — pending user OAuth app registration.)
