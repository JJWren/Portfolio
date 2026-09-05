# Unit 10 owner deploy checklist (BJJ landing flavor)

**Written**: 2026-09-04, after release v1.25.1. **Completed by the owner**: 2026-09-04 (Steps 0 to 4 confirmed; Steps 5 and 6 are optional and not separately confirmed). **For**: the owner, at the production host and at `/admin/site`. **Source of every value**: the content sheet in `construction/plans/unit10-bjj-landing-plan.md` (copied exactly; drafts are marked), `.env.example`, `README.md`. Nothing here is code: these are the owner's own words and settings, entered as content (BR-18).

Work top to bottom. Each step says what to check before moving on.

## Step 0: run the release that has the flavor

The BJJ flavor shipped in release v1.25.1 (2026-09-04). The container must run that image or newer before any of the settings below do anything.

On the production host, in the deployment folder:

```bash
docker compose pull web
```

```bash
docker compose up -d web
```

The three Unit 10 migrations (`AddBjjLandingCopy`, `AddRoadAndNow`, `AddOwnerPhotoFlipAlt`) run at startup.

- [x] Check: `https://joshuamykitta.dev/healthz` is healthy and the landing page still renders the plain layout (the flavor is not switched on yet).

## Step 1: the two env-only settings

Both keys are startup configuration, not admin-editable. Compose reads the production `.env` through `env_file`, so adding the keys there is enough.

Add to the production `.env`:

```text
SITE_FLAVOR=bjj
OWNER_PHOTO_FLIP_FILE=/app/uploads/owner-mat.jpg
```

Rules for the second value:

- It is a path inside the container, on the same read-write folder as the current `OWNER_PHOTO_FILE`. The `uploads` volume is mounted at `/app/uploads`, so if the primary photo is `/app/uploads/owner.jpg`, a sibling such as `/app/uploads/owner-mat.jpg` is right. If the primary photo lives somewhere else, put the mat photo next to it instead.
- The file does not have to exist yet. The upload in Step 4 creates it.
- The mat photo needs the primary photo: the two-photo switch appears only when both slots resolve.

Then recreate the container so the new env is read:

```bash
docker compose up -d web
```

- [x] Check: the landing page now shows the BJJ layout with only the sections that already have content (hero with the existing heading and tagline, About, Skills). Empty BJJ sections stay hidden; that is expected until Step 3.

## Step 2: `/admin/site`, the existing fields

Sign in, open `/admin/site`. The top group is the same as before.

| Field on the page | Paste |
|---|---|
| Hero heading (the landing-page H1) | `Position before submission.` |
| Tagline (also the meta description) | `A game plan borrowed from the mat: secure the application first, build the tooling that improves position, then finish. Secure software engineer; Brazilian jiu-jitsu black belt.` |

The tagline is a draft: change it any time; the page saves whatever you type.

## Step 3: `/admin/site`, the BJJ group

This group is visible only because `SITE_FLAVOR=bjj` is set. Multi-line fields take one item per line with real line breaks (press Enter), fields inside a line separated by ` | `.

| Field on the page | Paste |
|---|---|
| Hero eyebrow (line above the hero heading) | `Joshua Mykitta · Software Engineer` |
| Belt caption (shown under the rank bar) | `Black belt · Championship Mixed Martial Arts, Daphne, AL · promoted December 9, 2025 by Rodney Souza` |
| Belt degrees (stripes on the rank bar, 0 to 6) | `0` |

Game plan (exactly four lines; the readings are drafts):

```text
Guard | Secure the position | Auth, secrets, boundaries. Get the position first.
Pass | Improve the position | Developer tooling that makes the next move easier.
Mount | Keep control | Tests, reviews, boring deploys.
Submit | Finish | Ship it, then write it up.
```

Principles (three lines; the readings are drafts):

```text
Position before submission. | Security before features. Lock the position down, then attack the problem.
Tap early, tap often. | Roll back early. A revert costs nothing; an outage does.
Leave your ego at the door. | Code review is not a fight. The best idea wins, not the loudest.
```

Eras (five lines, in the order they should appear; nothing re-sorts them):

```text
2005-12-01 | white | 2 | Tamaso's MMA | Fairhope, AL | High school junior, working at Baumhower's Wings.
2018-01-30 | blue | 3 | Bagram BJJ | Bagram, Afghanistan | Veteran and contractor in Afghanistan, keeping Army helicopters in top shape as an aviation backshop mechanic.
2019-08-23 | purple | 1 | Bagram BJJ | Bagram, Afghanistan | Backshop manager leading multiple shops and 20+ personnel, and teaching BJJ to base personnel at night.
2020-09-23 | brown | 4 | Iron Grip BJJ | Fairhope, AL | Transitioning into tech through a University of Arizona Cyber Operations degree and the Coding Dojo coding bootcamp.
2025-12-09 | black | 0 | Championship Mixed Martial Arts | Daphne, AL | Software Engineer at Acentra Health, building healthcare software that integrates care management, utilization management, and prior authorization requests into a single system.
```

Now (four lines; Building and Home lab are drafts from the repo descriptions):

```text
Teaches | Adult no-gi, Tuesday and Thursday mornings, at Championship Mixed Martial Arts
Building | CalCrony: a self-hosted Discord bot for events and RSVPs, with reminders, ICS feeds and Google Calendar free/busy checks. The .NET 10 API is the product; the bot is just a client.
Home lab | Containerized, locked down, and built for high availability. This site runs on it.
Household | Wife, 5 kids, 6 dogs
```

Save.

- [x] Check: the save succeeds. The only rule that can refuse it is the degrees check: Belt degrees `0` must equal the black era's stripes, which are `0` above, so they agree.
- [x] Check: the landing page shows the eyebrow, the four-node game plan, the rank bar (black belt, no stripes) with its caption, Principles (three cards), The road (a ladder of five belts and a five-row table), and Now (four tiles).

## Step 4: the mat photo and both alt texts

Still on `/admin/site`. The "Mat photo" group is visible because `OWNER_PHOTO_FLIP_FILE` is set.

1. In the Mat photo group, choose "Upload photo" and pick `aidlc-docs/inception/application-design/landing-directions/josh-mat.jpg` from the repo checkout (the 720px downsample). Keep the full-resolution original on your own machine; it does not go on the server.
2. Paste the alt texts:

| Field on the page | Paste |
|---|---|
| Photo alt text (the desk photo) | `Joshua Mykitta in sunglasses and a tuxedo, looking slightly off to the left with a subtle, pensive smile` |
| Mat photo alt text | `Joshua Mykitta, on the left, being promoted to Brazilian jiu-jitsu black belt by Rodney Souza, on the right` |

Save.

- [x] Check: the hero shows the desk photo with the "At the desk" / "On the mat" switch under it; choosing "On the mat" crossfades to the promotion photo. On a phone-width screen the photo sits above the hero text with the switch centered under it.
- [x] Check: the light theme keeps the belt black and the ladder colors unchanged (the belt colors are fixed constants, not theme tokens).
- [x] Check: `/admin/theme` previews the BJJ sections inside its preview frame.

## Step 5 (optional): align the local `.env`

From the plan's Phase 0. Only if you want the local run to match production:

```text
PUBLIC_BASE_URL=https://joshuamykitta.dev
SEED_DEMO_DATA=false
```

plus the live tagline and skills. Nothing in Unit 10 depends on this.

## Step 6 (your Windows machine, one time): the `python3` alias

Settings > Apps > Advanced app settings > App execution aliases: turn `python3.exe` off. This keeps a Store package update from bringing back the alias that made the security-guidance hooks loop on 2026-09-04.

## Rollback

Set `SITE_FLAVOR=` (blank) or remove the line, then:

```bash
docker compose up -d web
```

The plain landing page returns unchanged. Everything saved at `/admin/site` stays in the database, unused, until the flavor is switched back on.

## Not covered here

The env-only route for the copy (every `SITE_*` value in `.env` with lines joined by the literal two characters `\n`) works too, but the admin page is easier and an admin value always wins over the env value. Use the env route only if you ever want the content to travel with the `.env` file.
