# Unit 5 Plan — Comments + Moderation

Signed-in users comment on published posts (plain text, auto-encoded — no markdown/XSS surface).
Authors can delete their own; admins can hide/unhide or delete any, inline and from a
moderation page. Hidden comments stay in the DB but vanish from public view.

- [x] Comment entity (post FK cascade, user FK, body ≤2000, CreatedAt, IsHidden) + AddComments migration
- [x] CommentRules (trim/validate body) + 4 tests (38 total green)
- [x] CommentService (visible-for-post, all-for-admin, add, delete-own-or-admin, toggle hidden)
- [x] CommentSection component (InteractiveServer) on the post page: list with author + timestamp,
      form when signed in, sign-in prompt with returnUrl otherwise, own-delete + admin inline controls
- [x] Admin /admin/comments moderation table (post, author, body, state; hide/unhide/delete)
- [x] Dashboard comments card links to moderation page
- [x] Comment styles on existing tokens
- [x] Verify: compose up; seeded comment renders with author; anonymous sees sign-in prompt;
      IsHidden=true removes it publicly and empty-state appears (comment posting UI itself
      needs OAuth sign-in — pending user creds)
