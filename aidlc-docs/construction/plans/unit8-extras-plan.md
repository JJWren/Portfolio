# Unit 8 Plan — Extras, Polish, CI

- [ ] /feed.xml RSS 2.0 (published posts; PUBLIC_BASE_URL env or request host for absolute links)
- [ ] /sitemap.xml (static pages + published posts) and /robots.txt (disallow /admin, /auth, /signin)
- [ ] SEO meta: description + OpenGraph/Twitter tags on Home, Blog, BlogPost (article + og:image),
      Projects, Contact via HeadContent; RSS alternate link in App head
- [ ] SEED_DEMO_DATA=true startup seeder (sample posts + projects when tables are empty)
- [ ] /healthz health endpoint (with DB check) + curl in image + compose healthcheck on web
- [ ] GitHub Actions: build + test on push/PR; buildx publish to GHCR on master + v* tags
- [ ] README: full self-hosting guide (env table, OAuth callback URLs, reverse proxy, health, feed)
- [ ] Create GitHub repo JJWren/Portfolio (unset GITHUB_TOKEN first), initial commit, push
- [ ] Verify: build + tests green; compose: feed/sitemap/robots/healthz respond; seeder works on
      wiped volumes; CI workflow lints (yaml) — first Actions run happens on push
