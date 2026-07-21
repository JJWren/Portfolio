# Unit 8 Plan — Extras, Polish, CI

- [x] /feed.xml RSS 2.0 (published posts; PUBLIC_BASE_URL env or request host for absolute links)
- [x] /sitemap.xml (static pages + published posts) and /robots.txt (disallow /admin, /auth, /signin)
- [x] SEO meta: description + OpenGraph on Home and BlogPost (article + og:image), og:site_name +
      RSS alternate link in App head
- [x] SEED_DEMO_DATA=true startup seeder (sample posts + projects when tables are empty)
- [x] /healthz health endpoint (with DB check) + curl in image + compose healthcheck on web
- [x] GitHub Actions: build + test on push/PR; buildx publish to GHCR on master (latest) + v* tags
- [x] README: full self-hosting guide (env table, OAuth callback URLs, reverse proxy, ops, dev)
- [x] Created public repo JJWren/Portfolio, initial commit (97 files), pushed; first CI run in progress
- [x] Verify: 41 tests green; clean-slate compose (down -v → up): migrations + demo seed worked,
      feed/sitemap/robots/healthz all 200, web + db containers report healthy
