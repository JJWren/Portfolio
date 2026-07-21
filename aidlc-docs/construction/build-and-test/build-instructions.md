# Build Instructions

## Prerequisites
- .NET 10 SDK 10.0.302+ (pinned in `global.json`; `winget install Microsoft.DotNet.SDK.10`)
- Docker Desktop (compose v2) for container builds and the database

## Local build
```bash
dotnet build            # solution: Portfolio.slnx (web + tests)
```
The repo treats warnings as errors in CI: `dotnet build -warnaserror` must be clean.

## Container build
```bash
docker compose build    # multi-stage Dockerfile (sdk:10.0 → aspnet:10.0)
```
The runtime image adds `libgssapi-krb5-2` (Npgsql) and `curl` (healthcheck).

## Full stack
```bash
cp .env.example .env    # then set POSTGRES_PASSWORD at minimum
docker compose up -d --build
```
EF migrations apply automatically at startup; no manual DB steps.

## CI
`.github/workflows/ci.yml` restores, builds (`-warnaserror`), tests on every push/PR to
master, and publishes `ghcr.io/jjwren/portfolio` (`latest` on master, semver on `v*` tags).
