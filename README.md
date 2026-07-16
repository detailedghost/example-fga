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

## Frontend example (pure JS)

To show the same authorization from a framework-free frontend, `/access.html`
is a static page (plain HTML + vanilla JS, no Razor, no build step) that renders
the role matrix and flips roles on/off. It reads state from `GET /api/access` and
toggles via the same `/admin/access/grant` and `/revoke` endpoints — so changes
appear in the server-rendered `/Admin/Access` page too, since both hit the same
OpenFGA store. The roles list is fed from the API, not hardcoded, and `/api/*`
endpoints return `401`/`403` for the frontend rather than redirecting to login.

Sign in as an admin (`alice`), then open <http://localhost:5080/access.html>.

## Configuration

Everything is environment-driven via `.env`. The Postgres host port defaults to
`5434` (`POSTGRES_PORT`) to avoid colliding with a local Postgres on 5432 —
change it there if you prefer, and update `BLOG_DB_CONNECTION` to match.
