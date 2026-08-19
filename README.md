# FGA Blog POC

> ⚠️ **This is a dummy demo app — not production software.** It runs entirely on
> localhost with **fake application credentials** (passwords equal usernames).
> It has no real data or security hardening and exists only to compare fine-grained
> authorization providers. Never deploy it, reuse its credentials, or put AWS
> credentials in the committed `.env`.

A .NET 10 minimal-API blog demonstrating fine-grained authorization with
[OpenFGA](https://openfga.dev/) (self-hosted),
[Okta FGA](https://www.okta.com/products/fine-grained-authorization/) (the same
engine, hosted), or
[Amazon Verified Permissions](https://aws.amazon.com/verified-permissions/).
All three providers share one `IPermissionService`; set one environment
variable and restart to switch. Posts are protected by nested reader, writer,
editor, and admin roles evaluated by the selected store.

## Prerequisites

- .NET 10 SDK
- Docker (with Compose)
- For AWS mode: AWS CLI v2 with policy-store alias support, `jq`, and configured
  AWS credentials

## Running it

All application configuration lives in `.env` at the project root; there is no
`appsettings.json`.

### OpenFGA (default)

```bash
docker compose up -d   # provisions Postgres + the OpenFGA store (see Bootstrap)
dotnet run             # the app only resolves the store; it doesn't provision it
```

Or provision, wait, and run with:

```bash
./run.sh
```

Start `docker compose up` before `dotnet run`. Re-running `docker compose up`
while the app runs gives the FGA store a new id, so restart `dotnet run`
afterward to pick it up.

### Amazon Verified Permissions

Use a real AWS account. The bootstrap uses the normal AWS CLI credential chain
and the application uses the normal AWS SDK credential chain; `.env` contains
only the non-secret region and policy-store alias.

The AVP bootstrap requires AWS CLI policy-store alias commands. Older AWS CLI
v2 releases, including `2.30.4`, do not provide them. On Linux x86_64, update
the default AWS CLI v2 installation before bootstrapping:

```bash
aws_cli_tmp="$(mktemp -d)"
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" \
  -o "$aws_cli_tmp/awscliv2.zip"
unzip -q "$aws_cli_tmp/awscliv2.zip" -d "$aws_cli_tmp"
sudo "$aws_cli_tmp/aws/install" \
  --bin-dir /usr/local/bin \
  --install-dir /usr/local/aws-cli \
  --update
rm -r "$aws_cli_tmp"

aws --version
aws verifiedpermissions get-policy-store-alias help >/dev/null
aws sts get-caller-identity
```

Use the corresponding
[AWS-provided ARM installer](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
on Linux ARM, or follow the same AWS guide when the CLI uses non-default install
directories. The final two commands confirm that the CLI supports the required
AVP API and that AWS credentials are available.

```bash
# In .env:
AUTHORIZATION_PROVIDER=verifiedpermissions

./db/avp/bootstrap.sh  # creates or safely reconciles the AWS policy store
./run.sh               # starts only Postgres/Flyway, then the app
```

The bootstrap updates the Cedar schema, role templates, and owner policy and
ensures the five seed grants exist. It preserves any additional grants made in
the demo. Restore `AUTHORIZATION_PROVIDER=openfga`, then restart to switch back.
The stores stay independent; grants and revokes are not mirrored between them.

### Okta FGA

Okta FGA is OpenFGA hosted by Okta. It speaks the same wire API, so it needs no
new provider class — `FgaService` and `FgaStoreResolver` are reused unchanged,
and only the client configuration differs. The hosted API requires OAuth
client-credentials instead of the local server's anonymous access.

Create a store in the Okta FGA dashboard, then fill in the six `OKTA_FGA_*`
values in `.env`:

```bash
# In .env:
AUTHORIZATION_PROVIDER=oktafga
OKTA_FGA_API_URL=https://api.us1.fga.dev
OKTA_FGA_STORE_NAME=fga-blog-poc
OKTA_FGA_API_TOKEN_ISSUER=fga.us.auth0.com
OKTA_FGA_API_AUDIENCE=https://api.us1.fga.dev/
OKTA_FGA_CLIENT_ID=…
OKTA_FGA_CLIENT_SECRET=…
```

The app needs all six values, and throws at startup when one is missing. Apply
`db/fga/model.dsl` and `db/fga/seed.json` to the hosted store through the
dashboard or the FGA CLI — `db/fga/migrate.ts` targets the local container and
drops the store on each run, so do not point it at a hosted store.

`/playground.html` is OpenFGA-only and does not read the hosted store. Use the
Okta FGA dashboard instead.

## Bootstrap (the `db/` folder)

Provisioning is declarative and lives outside the app, in `db/`:

| Path | Purpose |
| --- | --- |
| `db/postgres/V*.sql` | schema + seed, applied by Flyway |
| `db/fga/model.dsl` | authorization model (DSL) |
| `db/fga/seed.json` | seed tuples |
| `db/fga/migrate.ts` | Bun migration that loads the two files via the API |
| `db/avp/schema.json` | Cedar model |
| `db/avp/templates/*.cedar` | named AVP role policy templates |
| `db/avp/policies/*.cedar` | shared owner policy |
| `db/avp/bootstrap.sh` | AWS CLI store reconciler |

On `docker compose up`: Flyway applies the SQL migrations (idempotent via its
schema-history table), then the `fga-migrate` service runs `db/fga/migrate.ts`
(a Bun script in an `oven/bun` container) — it drops any prior same-named store
and recreates it fresh from `model.dsl` + `tuples.json`. The app then resolves
the store by name at startup (read-only) — no schema creation or tuple seeding
in app code.

The AVP bootstrap resolves a stable `policy-store-alias/...`, creates the store
when absent, applies the schema before enabling strict validation, updates the
named policies/templates, and adds missing seed policies. AVP is eventually
consistent, so the application briefly polls after role writes before refreshing
the access matrix.

## URLs

| Service | URL |
| --- | --- |
| Blog app | <http://localhost:5080> |
| Local FGA playground | <http://localhost:5080/playground.html> |
| OpenFGA API | <http://localhost:8080> |
| OpenFGA Playground (blocked, see below) | <http://localhost:3000/playground> |

Use the AWS console to inspect the AVP store.

### The bundled Playground doesn't work — use `/playground.html`

`:3000/playground` is not a local UI. It's a one-line HTML shim whose body is an
`<iframe>` pointing at the **hosted** `https://play.fga.dev/sandbox/`, passing
`?fga_api_host=127.0.0.1:8080`. So a public HTTPS page has to call back into your
private network, and Chrome's **Private Network Access** policy blocks that unless
the local server opts in on preflights with
`Access-Control-Allow-Private-Network: true`. OpenFGA v1.8.0 omits that header:

```bash
curl -si -X OPTIONS http://localhost:8080/stores \
  -H 'Origin: https://play.fga.dev' \
  -H 'Access-Control-Request-Method: GET' \
  -H 'Access-Control-Request-Private-Network: true' | grep -i private-network
# (no output — the opt-in header is absent)
```

The frame therefore loads its own stock sandbox and never sees `fga-blog-poc`.
Ordinary CORS is fine (`Access-Control-Allow-Origin: *`); this is a separate,
stricter browser policy, so it can't be fixed from the app side.

In OpenFGA mode, `/playground.html` replaces the hosted page. The browser serves
it from localhost, so the public-to-private rule does not apply. It reads the store
coordinates from `GET /api/fga/store` (keeping
the API URL in `.env` rather than hardcoded in JS), then renders the model, every
tuple, and a Check runner straight off the OpenFGA API. Sign in first — it requires
an authenticated session like the other `/api/*` endpoints.

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

Roles are nested (admin ⊃ editor ⊃ writer ⊃ reader). OpenFGA models them as
relations in `db/fga/model.dsl`; AVP models the same inheritance as Cedar action
groups and stores direct role grants as template-linked policies. For AVP post
checks, the application sends the Postgres author as the Cedar `Post.owner`
attribute, while OpenFGA stores an owner tuple.

| Role | Read | New | Edit own | Edit any | Del own | Del any | Manage |
| --- | :-: | :-: | :-: | :-: | :-: | :-: | :-: |
| reader | ✅ | | | | | | |
| writer | ✅ | ✅ | ✅ | | ✅ | | |
| editor | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| admin | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

ASP.NET Core owns identity and the policy gate; every decision is delegated to
the selected `IPermissionService`. Admins manage roles at `/Admin/Access`, which
writes OpenFGA tuples or AVP template-linked policies through that same interface.

## Frontend examples (pure JS)

Two framework-free static pages (plain HTML + vanilla JS, no Razor, no build step)
show the selected provider's authorization from the browser:

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

Both return identical responses, including a `provider` field. The role →
permission mapping lives behind `IPermissionService`; the frontend never
hardcodes it.

Sign in as an admin (`alice`), then open <http://localhost:5080/access.html> or
<http://localhost:5080/actions.html>.

## Configuration

Everything is environment-driven via `.env`:

| Setting | Meaning |
| --- | --- |
| `AUTHORIZATION_PROVIDER` | `openfga`, `oktafga`, `verifiedpermissions` |
| `FGA_API_URL`, `FGA_STORE_NAME` | Used only in OpenFGA mode |
| `OKTA_FGA_API_URL`, `OKTA_FGA_STORE_NAME` | Required in Okta FGA mode |
| `OKTA_FGA_API_TOKEN_ISSUER`, `OKTA_FGA_API_AUDIENCE` | Okta FGA mode |
| `OKTA_FGA_CLIENT_ID`, `OKTA_FGA_CLIENT_SECRET` | Okta FGA mode; secret |
| `AWS_REGION`, `AVP_POLICY_STORE_ID` | Required only in AVP mode |
| `AWS_PROFILE` | Optional AWS profile override |

`AUTHORIZATION_PROVIDER` defaults to `openfga`.

Change `POSTGRES_PORT` and the port in `BLOG_DB_CONNECTION` together if the
configured host port collides locally.

The application identity needs `verifiedpermissions:GetPolicyStore`,
`IsAuthorized`, `ListPolicies`, `CreatePolicy`, and `DeletePolicy` for the selected
store. The bootstrap identity additionally needs policy-store/alias creation,
`PutSchema`, `UpdatePolicyStore`, and policy/template get/create/update actions.

## Tests

```bash
dotnet test tests/FgaPoc.Tests/FgaPoc.Tests.csproj
```

The tests exercise the shared role model and the real AVP provider through its
narrow SDK seam, including Cedar request entities, allow/deny handling, role
hierarchy, pagination, and template-linked grants.
