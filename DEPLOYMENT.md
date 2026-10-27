# Deploying the Eshop backend with Dokploy

## What runs where

| Layer | Where |
|---|---|
| Catalog, Inventory, Orders, Notification, Gateway | **VPS** — Dokploy service `eshop`, `deploy/docker-compose.yml` |
| Keycloak | **VPS** — separate Dokploy service built from the private theme repo (§7) |
| Traefik (TLS + routing) | **VPS** (installed and managed by Dokploy) |
| PostgreSQL | **External / managed** (Neon, Supabase, RDS, ...) |
| MongoDB | **External / managed** (Atlas, ...) |
| Redis | **External / managed** (Upstash, Redis Cloud, ...) |
| RabbitMQ | **External / managed** (CloudAMQP, ...) |
| Frontend | **Vercel** (rewrites `/api` to the VPS) |
| Payment | **Not deployed** |

Payment is intentionally **not** deployed (it is no longer in the solution/AppHost). The gateway still has a `/api/Payment` route, so requests to it return `502`. COD orders never touch Payment; PayPal orders will not complete.

There are **two independent Dokploy services**, each with its own Git repo and compose file:

| Service | Containers | Repo |
|---|---|---|
| `eshop` | catalog, inventory, orders, notification, gateway | this repo |
| `keycloak` | keycloak | private theme repo |

For this repo, the only deployment file you need is `deploy/docker-compose.yml`, plus the environment variables from `deploy/.env.example`. Keycloak's Dockerfile, compose file, and variables are in §7.

---

## 1. What was missing

These were the blockers between the repository and a working Dokploy deployment:

| # | Missing step | Why it mattered | Fixed in this repo |
|---|---|---|---|
| 1 | Dockerfiles restored before copying `Directory.Packages.props` and the `ServiceDefaults`/`Events` project files | `docker build` failed at `dotnet restore`; no images could be produced | All Dockerfiles rewritten |
| 2 | Notification had no Dockerfile | The service could not be containerized | `Eshop.Notification/Dockerfile` added |
| 3 | No root `.dockerignore` | The whole repo (including `.git`, `node_modules`, `bin`, `obj`) was sent as build context; local `appsettings.json` secrets could be baked into images | `.dockerignore` added |
| 4 | No production compose | Nothing tied the services, Keycloak, and Dokploy routing together | `deploy/docker-compose.yml` added |
| 5 | Health endpoints were only mapped in Development (`/health`, `/alive`) | Container health checks and deploy waits could not work | `AddServiceDefaults()` + `MapDefaultEndpoints()` now run in every environment |
| 6 | `UseHttpsRedirection()` ran in HTTP-only containers behind a proxy | Redirects/loops and failing health checks | HTTPS redirection disabled in Production; TLS terminates at Traefik |
| 7 | Gateway used Aspire service names and `https://` destinations in Production | YARP could not resolve `catalogApi`, `orderApi`, etc. outside Aspire | Compose overrides all cluster destinations with Docker DNS + HTTP |
| 8 | Gateway trusted only one hard-coded proxy IP and a hard-coded CORS origin | Wrong client IPs for rate limiting, CORS failures after moving hosts | Trusted proxies, `ForwardLimit`, and CORS origins are configurable via environment variables |
| 9 | Keycloak was only ever started with `start-dev`, no production DB/TLS/realm import | Login would not work on a real hostname | Keycloak 26 uses managed PostgreSQL, HTTPS hostname, proxy headers, and your realm export + theme baked into a private image (§7) |
| 10 | No TLS / public routing | Keycloak requires HTTPS in production | Traefik labels in the compose file + Dokploy's Let's Encrypt |
| 11 | Secrets lived in local, git-ignored `appsettings.json` | A fresh clone has no config at all | All runtime config comes from environment variables |
| 12 | Managed databases did not exist / were not reachable from the VPS | Services crash or time out at startup | Setup documented in §5 |
| 13 | No TLS settings on the connection strings | Managed providers reject plaintext connections | `SSL Mode=Require` inside `PG_CONNECTION`, plus `ssl=True` and `amqps` |
| 14 | Catalog never applied its EF outbox migration | Publishing events from Catalog fails on a fresh database | `Database.Migrate()` added at startup |
| 15 | No deployment automation | Every release was manual | Dokploy deploys on demand or on Git push |
| 16 | Payment excluded | Online-payment orders cannot complete; COD still works | Documented, not deployed |

> **Before anything else:** the working tree contains untracked source files (`Directory.Packages.props`, the new DTO folders, `CollectionService`, etc.). Dokploy clones from Git, not from your desktop. Commit and push everything first, and make CI green.

---

## 2. Architecture

```text
                    Browser  /  Vercel frontend
                              |
                        HTTPS | (80/443)
                              v
                  +------------------------+
                  |  Dokploy -> Traefik    |  Let's Encrypt
                  +-----------+------------+
                              |
          +-------------------+-------------------+
          |                                       |
   api.<host>                              auth.<host>
          |                                       |
  [ eshop service ]                     [ keycloak service ]
   gateway:8080                          keycloak:8080
          |                                       |
   +------+------+------+------+                 |
   |      |      |      |      |                 |
catalog inventory orders notif. |                 |
   |      |      |              |                 |
   +------+------+--------------+-----------------+
          |      |              |
          |      |              +--> PostgreSQL (managed): Catalog outbox,
          |      |                                         Inventory, Orders,
          |      |                                         Keycloak
          |      +------------------> MongoDB (managed): Catalog data
          |
          +-------------------------> Redis (managed) + RabbitMQ (managed)
```

Only Traefik listens on public ports. Keycloak and the services run in two separate Docker projects, each on its own private network, and reach the managed services over TLS on the public internet. The .NET services reach Keycloak through `https://${AUTH_DOMAIN}` (host-gateway), not through a shared Docker network.

---

## 3. Prerequisites

- Ubuntu 22.04/24.04 VPS with **4 GB RAM recommended** (2 GB minimum; Dokploy + 6 containers).
- SSH access and a sudo user.
- Ports `22`, `80`, `443` open. Port `3000` for the Dokploy panel (restrict it to your IP).
- Accounts on the managed providers (any equivalent works):
  - PostgreSQL: Neon, Supabase, Aiven, or RDS.
  - MongoDB: Atlas.
  - Redis: Upstash or Redis Cloud.
  - RabbitMQ: CloudAMQP.
- UploadCare and Brevo keys if you want image uploads and email.
- No domain yet? Use `sslip.io` for free hostnames:
  - `api.<VPS_PUBLIC_IP>.sslip.io`
  - `auth.<VPS_PUBLIC_IP>.sslip.io`

---

## 4. Create the managed services

### 4.1 PostgreSQL

The three .NET services share **one database** (`postgres`) and are kept apart by **schema**:

| Service | Schema | What it creates there |
|---|---|---|
| Catalog | `catalog` | `OutboxMessage`, `OutboxState`, `InboxState` |
| Inventory | `inventory` | `Inventories` |
| Orders | `orders` | `Carts`, `CartItems`, `Orders`, `OrderItems`, `OrderStates`, outbox tables |
| Keycloak | `public` | its own realm/client/user tables |

**You do not create anything manually.** Each service reads `Search Path` from its own connection string and runs `CREATE SCHEMA IF NOT EXISTS` before `Database.Migrate()`, so the first start creates its schema, its `__EFMigrationsHistory`, and its tables.

Why the separation is required: Catalog and Orders both create tables named `InboxState`, `OutboxMessage`, and `OutboxState`, and EF's `__EFMigrationsHistory` is per schema. Without it the second service to start fails with `relation "InboxState" already exists`. The compose file appends `Search Path=catalog` / `inventory` / `orders` to `PG_CONNECTION`, so **no migration change is needed**.

If you prefer to create them up front, it is harmless:

```sql
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS orders;
```

- **Supabase:** one project is enough. Use the **session pooler** (`aws-0-<region>.pooler.supabase.com`, port **5432**) and the pooler username `postgres.<project-ref>`. Port `6543` (transaction mode) breaks EF migrations. Run the `CREATE SCHEMA` statements in the SQL editor.
- **Neon:** run the statements against the default database (`neondb`); set `Database=neondb` inside `PG_CONNECTION` if you keep that name.
- **RDS / Aiven:** create the instance; connect to the default database and run the statements.
- Use the **direct** hostname where the provider offers one. EF Core migrations take a session-level advisory lock, which transaction-mode poolers do not preserve.
- Connection limits are shared by Catalog, Inventory, and Orders (10 each via `Maximum Pool Size=10`) plus Keycloak (capped at 10 via `KC_DB_POOL_MAX_SIZE`). Raise these only after checking the provider's limit.
- If you want stricter TLS, add `SSL Mode=VerifyFull` inside `PG_CONNECTION` (and `PG_JDBC_SSLMODE=verify-full` for Keycloak). `Require` encrypts without validating the certificate.
- Allow the **VPS public IP** on the provider if it enforces an allowlist.

> `Database.Migrate()` runs at startup for Catalog, Inventory, and Orders, so the schemas only need to exist and be empty. If your provider supports multiple databases and you prefer that, point `Database=` in `PG_CONNECTION` at that database and delete `;Search Path=...` from the three `ConnectionStrings__*` lines in the compose file — nothing else changes.

### 4.2 MongoDB (Atlas)

1. Create a cluster and a database user.
2. **Network Access → Add IP Address →** the VPS public IP (or `0.0.0.0/0` while testing).
3. Copy the `mongodb+srv://...` connection string and replace `<password>`.
4. URL-encode special characters in the password (`@` → `%40`, `:` → `%3A`, `#` → `%23`, `/` → `%2F`).

The database name is the path segment of the URI — `…mongodb.net/Eshop?retryWrites=true` — and Atlas creates it (and the collections) on first write. `MONGO_DATABASE` is an optional override if you prefer to keep the URI path empty.

> `appName=Eshop` in an Atlas URI is only a label for the client in Mongo's logs and monitoring. It is **not** the database name.

### 4.3 Redis (Upstash or Redis Cloud)

Create a database with **TLS enabled** and note the host and password.

StackExchange.Redis wants the key/value form, not a `redis://` URL:

```text
REDIS_CONNECTION='host.upstash.io:6379,password=change-me,ssl=True,abortConnect=False'
```

### 4.4 RabbitMQ (CloudAMQP)

Create an instance and copy the AMQP URL. Use `amqps`, include the port, and remember the path is the virtual host:

```text
RABBITMQ_CONNECTION='amqps://user:password@host.cloudamqp.com:5671/vhost'
```

If CloudAMQP gives you `amqps://user:pass@host/vhost` without a port, add `:5671` explicitly.

### 4.5 Keycloak database

Keycloak uses the same managed PostgreSQL, in the **`public`** schema (the default), so it does not touch the `catalog` / `inventory` / `orders` schemas. Set `KEYCLOAK_DB=postgres` and, unless Keycloak has its own user, the same credentials as `PG_CONNECTION` — in the Keycloak service's environment (§7.3).

---

## 5. Environment variables

Start from the template:

```bash
cp deploy/.env.example deploy/.env
chmod 600 deploy/.env
```

Fill in:

- `API_DOMAIN`, `AUTH_DOMAIN` (no `https://`, no trailing slash)
- `FRONTEND_ORIGIN` — exact Vercel production origin
- `PG_CONNECTION` — one full Npgsql connection string (pasted as-is; the compose appends `Search Path`)
- `MONGO_CONNECTION` — include the database name in the path (`…/Eshop?retryWrites=true`); `MONGO_DATABASE` only if you want to override it
- `REDIS_CONNECTION`
- `RABBITMQ_CONNECTION`
- UploadCare and Brevo keys

Keycloak's own variables (`KEYCLOAK_ADMIN_*`, `KEYCLOAK_DB`, `PG_JDBC_SSLMODE`, ...) are **not** here — they belong to the separate Keycloak service, listed in §7.3.

Rules:

- Keep the quotes in the template. `MONGO_CONNECTION`, `REDIS_CONNECTION`, and `RABBITMQ_CONNECTION` contain `&`, `?`, or `,`.
- Do **not** use `$`, `'`, `#`, spaces, `;`, or `=` inside passwords. `$` breaks Compose interpolation; `'` breaks the quoting; `;` breaks Npgsql connection strings.
- Do **not** commit `deploy/.env`.
- Core variables (`API_DOMAIN`, `AUTH_DOMAIN`, `FRONTEND_ORIGIN`, `PG_CONNECTION`, `MONGO_CONNECTION`, `REDIS_CONNECTION`, `RABBITMQ_CONNECTION`) use `${VAR:?}`, so a missing or empty value **aborts the deploy** with `required variable X is missing a value` instead of starting a broken container.
- `UPLOADCARE_PUBLIC_KEY`, `UPLOADCARE_SECRET_KEY`, and `EMAIL_API_KEY` are intentionally blank in the template. They are **not** guarded: the stack starts without them, but image uploads and emails fail at runtime.

You will paste these values into the **Eshop** service's Environment tab in §8.

---

## 6. Install Dokploy

```bash
curl -sSL https://dokploy.com/install.sh | sh
```

Then:

1. Open `http://<VPS_IP>:3000` and create the admin account.
2. Firewall: allow `22`, `80`, `443` normally; allow `3000` only from your IP:
   ```bash
   sudo ufw allow OpenSSH
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw allow from <your-ip> to any port 3000 proto tcp
   sudo ufw enable
   ```
3. In Dokploy, open **Settings** and set the Let's Encrypt email address used by Traefik.
4. DNS: create `A` records for `api.<host>` and `auth.<host>` pointing at the VPS. With `sslip.io` there is nothing to create — the names resolve automatically.

> Dokploy installs Traefik and owns ports `80`/`443`. Do not install Caddy, Nginx, or another reverse proxy on the same VPS.

---

## 7. Keycloak as a separate Dokploy service

Keycloak is **not** part of the Eshop stack. It lives in its own **private Git repo** (Dockerfile + theme jar + realm export + its own compose file) and runs as a second Dokploy **Compose** service.

| Dokploy service | Git repo | Compose path | Containers |
|---|---|---|---|
| `eshop` | `Eshop-Backend` (this repo) | `deploy/docker-compose.yml` | catalog, inventory, orders, notification, gateway |
| `keycloak` | private Keycloak repo | `docker-compose.yml` | keycloak |

Each Dokploy Compose service is bound to exactly one Git repo, so the private repo builds its own Dockerfile directly: **no image registry and no `docker login`** are involved.

The two stacks do not share a Docker network. The .NET services reach Keycloak over its public URL (`https://${AUTH_DOMAIN}`), which `extra_hosts: ${AUTH_DOMAIN}:host-gateway` resolves to the VPS, where Traefik routes it. That is why the Eshop compose still needs `AUTH_DOMAIN`.

`extra_hosts` is not redundant with normal DNS. Without it the container resolves the public hostname to the VPS's **public** IP and has to hairpin-NAT back into the host — which AWS and some other clouds refuse to do, so the OIDC metadata fetch fails and every API request returns 401/500. Pinning the name to the host gateway makes the path deterministic. You may drop it only if you have verified that the VPS can reach its own public IP.

### 7.1 Dockerfile (private Keycloak repo)

```dockerfile
FROM quay.io/keycloak/keycloak:26.8.1 AS builder

# Theme + realm must exist before `kc.sh build` so they are augmented once at
# build time instead of on every container start.
COPY keycloak-theme.jar /opt/keycloak/providers/keycloak-theme.jar
COPY realm-export.json /opt/keycloak/data/import/realm-export.json

# Build-time options. They must match the runtime environment, otherwise
# `--optimized` refuses to start.
ENV KC_DB=postgres
ENV KC_HEALTH_ENABLED=true

RUN /opt/keycloak/bin/kc.sh build

FROM quay.io/keycloak/keycloak:26.8.1
# Re-declare, otherwise the image only starts with `--optimized` when these are
# also present at runtime.
ENV KC_DB=postgres
ENV KC_HEALTH_ENABLED=true
COPY --from=builder /opt/keycloak/ /opt/keycloak/

# No hostname, no URLs: everything environment-specific comes from Compose
# (KC_HOSTNAME, KC_DB_URL, KC_PROXY_HEADERS, ...).
ENTRYPOINT ["/opt/keycloak/bin/kc.sh"]
CMD ["start", "--import-realm", "--optimized"]
```

Differences from the Render version:

| Render Dockerfile | Why it changed |
|---|---|
| `--hostname=https://keycloakifyeshop.onrender.com` | Hard-coded. The hostname now comes from `KC_HOSTNAME=https://${AUTH_DOMAIN}`, so the same image works on any domain |
| `--http-enabled=true`, `--proxy-headers=xforwarded`, `--hostname-strict=false` on the CLI | Kept as `KC_HTTP_ENABLED` / `KC_PROXY_HEADERS` in the Compose environment |
| `RUN kc.sh build --health-enabled=true` | Health is a **build** option, so it is set as `KC_HEALTH_ENABLED` before `kc.sh build` |
| Single stage | Multi-stage + `--optimized` keeps startup at a few seconds instead of re-augmenting on every boot |
| `26.5.7` | `26.8.1` to match the deployment. If the Keycloakify theme renders wrong, rebuild the jar against the same version |

### 7.2 `docker-compose.yml` (private Keycloak repo)

```yaml
services:
  keycloak:
    build:
      context: .
      dockerfile: Dockerfile
    image: eshop-keycloak:${KEYCLOAK_VERSION:-26.8.1}
    restart: unless-stopped
    environment:
      # `:?` makes Compose refuse to deploy when a value is missing. Without it
      # the message becomes jdbc:postgresql://:/?sslmode= inside Keycloak, which
      # looks like a Keycloak bug but is really an empty variable.
      KC_BOOTSTRAP_ADMIN_USERNAME: ${KEYCLOAK_ADMIN_USER:?}
      KC_BOOTSTRAP_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD:?}
      KC_DB: postgres
      KC_DB_URL: "jdbc:postgresql://${PG_HOST:?}:${PG_PORT:?}/${KEYCLOAK_DB:?}?sslmode=${PG_JDBC_SSLMODE:?}"
      KC_DB_USERNAME: ${KEYCLOAK_DB_USER:?}
      KC_DB_PASSWORD: ${KEYCLOAK_DB_PASSWORD:?}
      KC_DB_POOL_INITIAL_SIZE: "2"
      KC_DB_POOL_MAX_SIZE: "10"
      KC_HOSTNAME: https://${AUTH_DOMAIN:?}
      KC_PROXY_HEADERS: xforwarded
      KC_HTTP_ENABLED: "true"
      KC_HEALTH_ENABLED: "true"
      KC_LOG_LEVEL: INFO
    healthcheck:
      test: ["CMD-SHELL", "bash -c 'exec 3<>/dev/tcp/127.0.0.1/9000'"]
      interval: 15s
      timeout: 5s
      retries: 20
      start_period: 90s
    labels:
      - "traefik.enable=true"
      - "traefik.docker.network=dokploy-network"
      - "traefik.http.routers.eshop-auth.rule=Host(`${AUTH_DOMAIN:?}`)"
      - "traefik.http.routers.eshop-auth.entrypoints=websecure"
      - "traefik.http.routers.eshop-auth.tls.certresolver=letsencrypt"
      - "traefik.http.services.eshop-auth.loadbalancer.server.port=8080"
    networks:
      - dokploy-network

networks:
  dokploy-network:
    external: true
```

No `ports:` — Traefik reaches it through `dokploy-network`, exactly like the Eshop gateway. Docker builds the image on the VPS from this repo; nothing is pushed anywhere.

If you would rather paste one value instead of five, replace the whole `KC_DB_URL` line with:

```yaml
      KC_DB_URL: "jdbc:postgresql://aws-0-eu-central-1.pooler.supabase.com:5432/postgres?sslmode=require"
```

### 7.3 Environment variables for the Keycloak service

These go in the **Keycloak** service's Environment tab. They are deliberately not in the Eshop stack:

```env
AUTH_DOMAIN=auth.203.0.113.10.sslip.io

KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=change-me-keycloak

PG_HOST=ep-cool-name-123456.eu-central-1.aws.neon.tech
PG_PORT=5432
PG_JDBC_SSLMODE=require

KEYCLOAK_DB=keycloak
KEYCLOAK_DB_USER=eshop
KEYCLOAK_DB_PASSWORD=change-me-postgres

KEYCLOAK_VERSION=26.8.1
```

If Keycloak uses the same database user as the services, `KEYCLOAK_DB_USER` / `KEYCLOAK_DB_PASSWORD` can be the same values.

### 7.3.1 If the database is Supabase

Supabase gives you a single database (`postgres`), which is exactly what the stack uses: Keycloak in `public`, the .NET services in `catalog` / `inventory` / `orders`. The provider-specific bits are:

```env
PG_HOST=aws-0-<region>.pooler.supabase.com
PG_PORT=5432
PG_JDBC_SSLMODE=require
KEYCLOAK_DB=postgres
KEYCLOAK_DB_USER=postgres.<project-ref>
KEYCLOAK_DB_PASSWORD=<database password>
```

- **Port `5432` is the session pooler — use that one.** Port `6543` is transaction mode; Keycloak's schema migration and advisory locks do not survive it.
- The pooler username is `postgres.<project-ref>`, not `postgres`.
- The .NET services use the same connection string (`PG_CONNECTION` in the Eshop service) — only their `Search Path` differs, and that is already in the compose file.
- Keycloak does not need a schema of its own: `public` holds no app tables, so there is nothing to collide with.

### 7.3.2 Do not add these

Two settings cause exactly the failure they look like they would fix:

| Don't set | Why |
|---|---|
| `KC_PROXY=edge` | Removed in Keycloak 26 and ignored. The compose file already sets `KC_PROXY_HEADERS=xforwarded`; adding both makes the proxy behaviour depend on which one wins |
| `--hostname-strict=false` / `KC_HOSTNAME_STRICT=false` | Only needed when the hostname is unknown. `KC_HOSTNAME` is set explicitly, so strict mode is correct and stops host-header attacks |

`KC_LOG_LEVEL=INFO` is right for production; `DEBUG` logs full request headers and grows quickly.

### 7.4 Deploy order

1. Deploy the **keycloak** service first and wait for it to be healthy:
   ```bash
   curl -fsS https://auth.<host>/realms/Eshop/.well-known/openid-configuration
   ```
2. Then deploy the **eshop** service.

After the first deploy the two are independent: redeploying Keycloak does not restart the API, and redeploying the API does not touch Keycloak. The only ordering that matters is the very first start, because the .NET services fetch the OIDC metadata from `https://auth.<host>`.

### 7.5 When the theme or realm changes

Commit to the private repo and redeploy **that** service only. The realm import only runs against an **empty** Keycloak database — later realm changes have to be applied in the admin console.

`Src/Services/Eshop.AppHost/realms/` still holds a copy of the theme jar and the realm export, but only for local Aspire development. The copies in the private repo are what production runs, so keep the two in sync.

Deleting or redeploying the Keycloak service never deletes data: the realm lives in the managed PostgreSQL (`KEYCLOAK_DB`).

---

## 8. Create the two Compose services in Dokploy

You will create **two** Compose services in the same project. Keycloak first (§7.4).

### 8.1 Keycloak

1. **Create Service → Compose**.
2. **Provider → Git → GitHub**: pick the **private** Keycloak repo and branch `main`.
   - If the repo does not appear, install/authorize the Dokploy GitHub App for that repository.
3. **Compose Path**: `docker-compose.yml` (from §7.2 — commit it to that repo).
4. **Environment**: the variables from §7.3.
5. **Domains**: the compose file already carries the `auth.<host>` Traefik labels. If you prefer the UI instead, delete the `labels:` block and add `auth.<host>` → service `keycloak`, port `8080`, HTTPS on. Never both.
6. **Deploy** and wait until it is healthy (first realm import takes 30–90 s).

### 8.2 Eshop

1. **Create Service → Compose**.
2. **Provider → Git**:
   - Repository: `https://github.com/OualidAissani/Eshop-Backend.git`
   - Branch: `main`
3. **Compose Path**: `deploy/docker-compose.yml`
   - The file uses `context: ..` for the five .NET builds, so it must be deployed from the repository layout. Do not upload a standalone copy of the YAML.
4. **Environment**: paste the full contents of your `deploy/.env`.
5. **Domains** — choose one approach, not both:
   - **Labels (already in the compose file):** nothing to do. `api.<host>` → `gateway:8080` with `certresolver=letsencrypt`.
   - **Dokploy UI:** delete the `labels:` block from the compose file, then add in the **Domains** tab `api.<host>` → service `gateway`, port `8080`, HTTPS on.
6. Optional: click **Preview Compose** to see how Dokploy injects its labels/network.
7. **Deploy** and watch the build logs. The first build produces five .NET images and takes several minutes.

---

## 9. Verify

### External connectivity

PostgreSQL:

```bash
# psql/pg_dump use the libpq URL, not the Npgsql string in PG_CONNECTION.
# Copy the URI from the provider dashboard (Supabase: Connect -> URI).
docker run --rm postgres:17-alpine \
  psql "postgresql://postgres.<project-ref>:<password>@aws-0-<region>.pooler.supabase.com:5432/postgres?sslmode=require" \
  -c '\dn'
```

Expected: the schemas `catalog`, `inventory`, `orders` (plus `public`). They are created by each service on startup; if one is missing, that service failed before migrating — check its logs.

Redis:

```bash
docker run --rm redis:7.4-alpine \
  redis-cli -h <redis-host> -p 6379 -a '<redis-password>' --tls ping
```

Expected: `PONG`.

If MongoDB or RabbitMQ fail, the deploy logs say so; the usual cause is the provider's IP allowlist.

### API health

```bash
curl -i https://api.<host>/health
```

Expected: `200 OK`.

### Keycloak / OIDC discovery

```bash
curl -fsS https://auth.<host>/realms/Eshop/.well-known/openid-configuration
```

Expected: JSON containing `"issuer":"https://auth.<host>/realms/Eshop"`.

### Gateway routing

```bash
curl -i https://api.<host>/api/Catalog
```

The Catalog list endpoint is public, so expect `200`. A `404` means Traefik is not routing the host; a `502` means the upstream service is down. To verify auth is wired up:

```bash
curl -i https://api.<host>/api/Order
```

Expect `401` without a bearer token.

---

## 10. Keycloak setup

1. Open `https://auth.<host>/admin`.
2. Log in with `KEYCLOAK_ADMIN_USER` / `KEYCLOAK_ADMIN_PASSWORD` from the environment variables.
3. Select the **Eshop** realm.
4. Create users and set permanent passwords.
5. Assign realm roles:
   - `Admin` for store management endpoints.
   - `Client` for normal users.
6. Open **Clients → eshop-frontend**:
   - Valid redirect URIs: your Vercel URL, e.g. `https://maktabahislamiia.vercel.app/*`
   - Web origins: your Vercel origin (the imported realm currently uses `*`; tighten it for production).
7. Confirm the client scope `eshop-api.all` is assigned so access tokens contain `aud: eshop-api`. The imported realm already has the audience mapper.

The realm is imported only when the Keycloak database is empty. If you change `realm-export.json` later, re-import/update through the admin console instead of expecting a restart to overwrite the database.

---

## 11. Point Vercel at the VPS

In `Src/Services/Eshop.Frontend/vercel.json`, change only the `/api` destination and **keep the SPA fallback rewrite**:

```json
{
  "rewrites": [
    { "source": "/api/:path*", "destination": "https://api.<host>/api/:path*" },
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

The file currently points at `https://eshop-gate.onrender.com`, so this is the line to change.

Add/update Vercel environment variables (Production + Preview):

```text
VITE_KEYCLOAK_URL=https://auth.<host>
VITE_KEYCLOAK_REALM=Eshop
VITE_KEYCLOAK_CLIENT=eshop-frontend
```

Redeploy the frontend. The frontend calls relative `/api/...`, so the rewrite sends it to the VPS gateway.

Make sure `FRONTEND_ORIGIN` exactly matches the browser origin (scheme + host, no trailing slash), then redeploy the stack in Dokploy.

### Rate limiting and client IP

The gateway partitions rate limits by client IP, taken from `X-Forwarded-For`. Two hops sit in front of it when the browser uses the Vercel rewrite:

```text
browser -> Vercel edge -> Traefik -> gateway
```

The default `ForwardLimit` is `1` (only Traefik is treated as a proxy), which means requests that arrive through Vercel are counted against Vercel's edge IP. If you expect real traffic, add this to the gateway environment:

```yaml
ForwardedHeaders__ForwardLimit: "2"
```

Leave it at `1` if the frontend calls the API host directly. Raising it while clients can reach the API directly lets them spoof `X-Forwarded-For` and bypass rate limits.

### Vercel preview deployments

Every Vercel preview gets a different origin (for example `https://eshop-backend-git-feature-user.vercel.app`), so it will **not** match the single `FRONTEND_ORIGIN` and requests from a preview will fail CORS.

Options:

- Test only against the production Vercel URL (simplest).
- Add each preview origin explicitly; the gateway reads an array, so the compose file supports indexed keys:

  ```yaml
  # deploy/docker-compose.yml → gateway.environment
  Cors__AllowedOrigins__0: ${FRONTEND_ORIGIN}
  Cors__AllowedOrigins__1: https://eshop-backend-git-feature-user.vercel.app
  ```

- Or use a wildcard preview domain (requires a code change: replace `WithOrigins(...)` with `SetIsOriginAllowed(...)` that checks the host suffix). Do not use `AllowAnyOrigin()` together with credentials.

---

## 12. Day-2 operations

### Deploy a new version

Each service deploys independently:

- **Eshop**: push to `main` of this repo → **Deploy** on the `eshop` service.
- **Keycloak**: push to the private repo → **Deploy** on the `keycloak` service.

A redeploy rebuilds changed images and recreates that stack's containers only.

### Environment changes

Editing a variable in Dokploy only saves it; **redeploy** for it to reach the containers. `AUTH_DOMAIN` exists in both services — change it in both, or the API validates tokens from the wrong issuer.

### Rollback

Dokploy keeps the deployment history for the service. Open the previous successful deployment and redeploy it. Database migrations are not automatically reversed — take a logical dump before schema changes.

### Logs

Use the **Logs** tab per service in Dokploy, or the built-in terminal.

### Backups

Neon, Atlas, Upstash, and CloudAMQP run their own backups — confirm the retention window on your plan. For a portable logical snapshot before a risky migration, run this from any machine with Docker and the repo:

```bash
set -a; source deploy/.env; set +a
PG_URL='postgresql://<user>:<password>@<host>:5432/postgres?sslmode=require'
STAMP=$(date -u +%Y%m%dT%H%M%SZ); mkdir -p backups/$STAMP

docker run --rm postgres:17-alpine \
  pg_dump "$PG_URL" --schema=catalog \
  --no-owner --no-privileges --clean --if-exists | gzip > backups/$STAMP/catalog.sql.gz

docker run --rm mongo:8 mongodump --uri "$MONGO_CONNECTION" --archive --gzip \
  > backups/$STAMP/mongo.archive.gz
```

Repeat the `pg_dump` line with `--schema=inventory` and `--schema=orders`. Keycloak lives in `public`, so dump it with `--schema=public` using the credentials from the Keycloak service's environment.

Restore (stop the services in Dokploy first, or scale them to zero):

```bash
gunzip -c backups/<stamp>/catalog.sql.gz | \
  docker run --rm -i postgres:17-alpine psql "$PG_URL"
```

The dump carries its `CREATE SCHEMA` statement, so restoring recreates `catalog` (or `inventory` / `orders`) before the tables.

### Disk usage

Dokploy keeps images and build cache. Use **Settings → Cleanup** in Dokploy, or on the VPS:

```bash
docker image prune -f
docker builder prune -f --keep-storage 5GB
```

There are no database volumes on the VPS, so clearing Docker data cannot delete your data.

---

## 13. Troubleshooting

| Symptom | Check |
|---|---|
| `dotnet restore` fails during the build | `Src/Services/Directory.Packages.props` must be committed; the Dockerfiles copy it before restore. |
| Dokploy build fails with "context not found" | **Compose Path** must be `deploy/docker-compose.yml` in the repository; the build context is `..` (repo root). |
| Domain returns `404` from Traefik | `gateway`/`keycloak` must be attached to `dokploy-network` (they are in the compose file); redeploy after changing domains; make sure you did not configure the domain both in labels and in the **Domains** tab. |
| Every domain 404s after a Docker upgrade | Dokploy's Traefik must be `v3.6.7` or newer on Docker Engine 28+. Update Dokploy/Traefik. |
| `required variable PG_CONNECTION is missing a value` | Compose refused the deploy. Add the variable from `deploy/.env.example` to the **Eshop** service's Environment tab |
| `28P01: password authentication failed` | Password mismatch or unescaped characters. Re-copy the credential; avoid `$ ' # ;`. |
| `too many clients already` / connection limit | Free tiers have low limits. The compose file caps the services at 10 connections each and Keycloak at 10; lower them further or upgrade. Do not switch to a transaction-mode pooler: EF migrations need a session-level lock. |
| `The server does not support SSL connections` | Set `SSL Mode=Disable` inside `PG_CONNECTION` (and `PG_JDBC_SSLMODE=disable` for Keycloak) only for a private/local server. |
| Mongo `Server selection timed out` | Atlas **Network Access** must include the VPS IP; URL-encode the password; check that `+srv` DNS works from the VPS. |
| Redis `NOAUTH` / TLS errors | `password=...` must be in `REDIS_CONNECTION`; managed Redis requires `ssl=True` and usually port `6379`. |
| RabbitMQ `ACCESS_REFUSED` / `NOT_ALLOWED` | The path in the URI is the **virtual host**; port must be `5671` for `amqps`. |
| `502` from the gateway | Check the target service's logs in Dokploy. `/api/Payment` is expected to be `502` because Payment is not deployed. |
| Keycloak service fails to build | The private repo must be authorized in Dokploy's GitHub App; the theme jar and `realm-export.json` must be committed next to the `Dockerfile` |
| `auth.<host>` returns 404 | Keycloak service must carry the Traefik labels from §7.2 (or the domain in the Domains tab, not both) and be on `dokploy-network` |
| The API returns 401/500 and the logs mention OIDC/`jwks` | Keycloak is not running or `AUTH_DOMAIN` is wrong in the **Eshop** service. Deploy Keycloak first (§7.4); inside a container test with `docker compose exec gateway curl -I https://$AUTH_DOMAIN/realms/Eshop` |
| Keycloak exits at startup with a build-option error | The image was built with different build-time options than the runtime environment. `--optimized` requires them to match (for example `KC_DB=postgres` at build and at runtime) |
| Login page shows the stock Keycloak theme | The theme jar is not in the image: `docker run --rm <image> ls /opt/keycloak/providers`. Also check the jar was built for this Keycloak version |
| `Unable to parse URL jdbc:postgresql://:/?sslmode=` | The variables never reached Compose. Check they are in the **Keycloak** service's Environment tab (not the Eshop one), saved **and** redeployed. The `:?` guards in §7.2 make this fail at deploy time instead |
| `no schema has been selected to create in` | The service could not create its schema: the database user lacks `CREATE` on the database, or the running image predates the `CREATE SCHEMA IF NOT EXISTS` call. Grant `CREATE` or create the schema manually (§4.1) |
| `relation "InboxState" already exists` | Two services are in the same schema. Check that each connection string carries its own `Search Path` |
| `database "keycloak" does not exist` | Set `KEYCLOAK_DB=postgres` — the stack uses one database for everything |
| Keycloak never becomes healthy | First import is slow. Check its logs; the health port is `9000` (`KC_HEALTH_ENABLED=true`). |
| Keycloak redirects to `http://` or the wrong host | `KC_HOSTNAME` must be `https://auth.<host>`; Traefik must pass `X-Forwarded-Proto`. Redeploy after changing it. |
| CORS error in the browser | `FRONTEND_ORIGIN` must exactly match the origin; redeploy the stack. |
| `401` for every authenticated request | Keycloak issuer/audience mismatch, clock drift, or missing `eshop-api.all` scope. Inspect the token with `jwt.ms`; verify `iss`, `aud`, and `role`. |
| Certificate not issued | DNS must resolve to the VPS, ports 80/443 must be reachable, Cloudflare must be DNS-only for the first issuance. |
| Uploads/emails fail but the API is up | Check `UPLOADCARE_*` and `EMAIL_*` values; the services start without them but external calls fail. |

---

## 14. Security checklist

- Rotate every key that was previously stored in local `appsettings.json`: UploadCare, Brevo, PayPal (if used later), and every database/broker password.
- Keep the Dokploy environment variables private; never commit `deploy/.env`.
- Expose only `22`, `80`, `443` publicly. Restrict `3000` to your IP or reach it over an SSH tunnel.
- On every managed provider, allowlist the **VPS public IP** rather than `0.0.0.0/0`.
- Use strong, unique passwords. URL-encode them in URIs, and avoid `$ ' # ;`.
- Restrict the Keycloak admin console (VPN, IP allowlist, or Traefik basic auth) if it is public.
- Enable the provider's backups/PITR.
- Keep the host and Dokploy patched.
- Add an uptime monitor against `https://api.<host>/health` and `https://auth.<host>/realms/Eshop/.well-known/openid-configuration`.
