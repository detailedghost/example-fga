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
docker compose up -d
dotnet run
```

Or, to do both and wait for OpenFGA to be ready first:

```bash
./run.sh
```

## URLs

| Service | URL |
| --- | --- |
| Blog app | <http://localhost:5080> |
| OpenFGA API | <http://localhost:8080> |
| OpenFGA Playground | <http://localhost:3000> |

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
relations. The authorization model is built in C# (`Fga/FgaModel.cs`); a
human-readable DSL mirror lives in `fga/model.fga`.

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
