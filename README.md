# FGA Blog POC

> ⚠️ **This is a dummy demo app — not production software.** It runs entirely on
> localhost with **fake, hard-coded credentials** (passwords equal usernames; the
> committed `.env` holds throwaway local values only). It has no secret hygiene,
> no real data, and no security hardening — it exists only to show OpenFGA
> fine-grained authorization. Never deploy it or reuse its credentials anywhere.

A .NET 10 minimal-API blog demonstrating fine-grained authorization with
[OpenFGA](https://openfga.dev/). Posts are protected by relationship-based
roles — reader, writer, editor, admin — evaluated on every request against
an OpenFGA store instead of hardcoded role checks.

## Prerequisites

- .NET 10 SDK
- Docker (with Compose)

## Running it

All configuration lives in `.env` at the project root — there is no
`appsettings.json`. Nothing else to edit.

```bash
docker compose up -d   # provisions Postgres + the OpenFGA store (see Bootstrap)
dotnet run             # the app only resolves the store; it doesn't provision it
```

Or, to do both and wait for OpenFGA to be ready first:

```bash
./run.sh
```

Start `docker compose up` before `dotnet run`. Re-running `docker compose up`
while the app runs gives the FGA store a new id, so restart `dotnet run`
afterward to pick it up.

## Bootstrap (the `db/` folder)

Provisioning is declarative and lives outside the app, in `db/`:

| Path | Purpose |
| --- | --- |
| `db/postgres/V*.sql` | schema + seed, applied by Flyway |
| `db/fga/model.dsl` | authorization model (DSL) |
| `db/fga/seed.json` | seed tuples |
| `db/fga/migrate.ts` | Bun migration that loads the two files via the API |

On `docker compose up`: Flyway applies the SQL migrations (idempotent via its
schema-history table), then the `fga-migrate` service runs `db/fga/migrate.ts`
(a Bun script in an `oven/bun` container) — it drops any prior same-named store
and recreates it fresh from `model.dsl` + `tuples.json`. The app then resolves
the store by name at startup (read-only) — no schema creation or tuple seeding
in app code.

## URLs

| Service | URL |
| --- | --- |
| Blog app | <http://localhost:5080> |
| OpenFGA API | <http://localhost:8080> |
| OpenFGA Playground | <http://localhost:3000/playground> |

The Playground is a web UI for browsing the store's model and tuples. Newer
OpenFGA releases deprecated it and bind it to container loopback (unpublishable),
so `docker-compose.yml` pins `openfga/openfga:v1.8.0`, which serves it on
`0.0.0.0:3000`.

## Viewing the FGA store

Besides the Playground, a small script prints the dummy store — its model
relations and every tuple — straight from the API:

```bash
bun run scripts/fga-view.ts
```

It shows the store, the authorization model's types/relations, and all tuples
grouped by object (role grants on `blog:main`, per-post `owner` links).

## Users

Seeded logins (password == username):

| Username | Role |
| --- | --- |
| `alice` | admin |
| `bob` | editor |
| `carol` | writer |
| `dave` | reader |
| `erin` | writer |

`carol` and `erin` are both writers so post ownership is demonstrable: a writer
can edit/delete only their own posts, while editors and admins act on any post.

## Roles & permissions

Roles are nested (admin ⊃ editor ⊃ writer ⊃ reader) and modeled as OpenFGA
relations. The authorization model lives in `db/fga/model.dsl` (DSL) and loads
into the store at startup via the Bun migration (`db/fga/migrate.ts`).

| Role | Read | New | Edit own | Edit any | Del own | Del any | Manage |
| --- | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| reader | ✅ | | | | | | |
| writer | ✅ | ✅ | ✅ | | ✅ | | |
| editor | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| admin | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

ASP.NET Core owns identity (cookie auth) and the policy gate; every
authorization decision is delegated to an OpenFGA `Check`. Admins manage
roles at `/Admin/Access`, which writes/deletes tuples live.

## Frontend examples (pure JS)

Two framework-free static pages (plain HTML + vanilla JS, no Razor, no build step)
show the same OpenFGA authorization from the browser:

- **`/access.html`** — the role matrix; flips roles on/off via `GET /api/access`
  and `POST /api/access/{grant,revoke}`. Changes appear in the server-rendered
  `/Admin/Access` page too, since both hit the same store. The roles list is fed
  from the API, not hardcoded. (Admin only.)
- **`/actions.html`** — a row of action buttons enabled/disabled by *your* current
  permissions. It reads `GET /mvc/me`, which returns the permissions you hold as
  `"action:resource"` strings (e.g. `create:posts`, `manage:access`); the frontend
  just checks membership with a small `can(permissions, "action:resource")` helper.
  Sign in as dave/carol/bob/alice to watch the buttons switch. (Any signed-in user.)

`/api/*` and `/mvc/*` endpoints return `401`/`403` for the frontend rather than
redirecting to the login page.

### API: minimal API and MVC, side by side

The JSON API is implemented twice on purpose, as a reference for both styles:

| Surface | Minimal API (`Endpoints/`) | MVC controller (`Controllers/`) |
| --- | --- | --- |
| Role matrix + grant/revoke | `/api/access…` | `/mvc/access…` |
| Current-user permissions | `/api/me` | `/mvc/me` |

Both return identical responses. The role → permission mapping lives only in
the API (`FgaService.GetPermissionsAsync`); the frontend never hardcodes it.

Sign in as an admin (`alice`), then open <http://localhost:5080/access.html> or
<http://localhost:5080/actions.html>.

## Configuration

Everything is environment-driven via `.env`. The Postgres host port defaults to
`5434` (`POSTGRES_PORT`) to avoid colliding with a local Postgres on 5432 —
change it there if you prefer, and update `BLOG_DB_CONNECTION` to match.
