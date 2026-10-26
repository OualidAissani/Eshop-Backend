# Deploying the Eshop backend with Dokploy

## What runs where

| Layer | Where |
|---|---|
| Catalog, Inventory, Orders, Notification, Gateway | **VPS** (Dokploy, Docker) |
| Keycloak | **VPS** (Dokploy, data in managed PostgreSQL) |
| Traefik (TLS + routing) | **VPS** (installed and managed by Dokploy) |
| PostgreSQL | **External / managed** (Neon, Supabase, RDS, ...) |
| MongoDB | **External / managed** (Atlas, ...) |
| Redis | **External / managed** (Upstash, Redis Cloud, ...) |
| RabbitMQ | **External / managed** (CloudAMQP, ...) |
| Frontend | **Vercel** (rewrites `/api` to the VPS) |
| Payment | **Not deployed** |

Payment is intentionally **not** deployed (it is no longer in the solution/AppHost). The gateway still has a `/api/Payment` route, so requests to it return `502`. COD orders never touch Payment; PayPal orders will not complete.

Dokploy runs **6 containers**: Keycloak and the five .NET services. There is no Caddy and no local database.

The only deployment file you need is `deploy/docker-compose.yml`, plus the environment variables from `deploy/.env.example`.

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
| 9 | Keycloak was only ever started with `start-dev`, no production DB/TLS/realm import | Login would not work on a real hostname | Keycloak 26 uses managed PostgreSQL, HTTPS hostname, proxy headers, and the committed realm import |
| 10 | No TLS / public routing | Keycloak requires HTTPS in production | Traefik labels in the compose file + Dokploy's Let's Encrypt |
| 11 | Secrets lived in local, git-ignored `appsettings.json` | A fresh clone has no config at all | All runtime config comes from environment variables |
| 12 | Managed databases did not exist / were not reachable from the VPS | Services crash or time out at startup | Setup documented in §5 |
| 13 | No TLS settings on the connection strings | Managed providers reject plaintext connections | `PG_SSL_MODE`, `ssl=True`, `amqps`, and `sslmode` are wired through the compose file |
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
   gateway:8080                            keycloak:8080
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

Only Traefik listens on public ports. Keycloak and the services live on a private Docker network and reach the managed services over TLS on the public internet.

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

Create one server/project and four databases:

```sql
CREATE DATABASE "EshopCatalogDb";
CREATE DATABASE "EshopInventoryDb";
CREATE DATABASE "EshopOrderDb";
CREATE DATABASE "keycloak";
```

- **Neon:** create one project and run the `CREATE DATABASE` statements against the default database.
- **Supabase:** run the statements in the SQL editor. On free plans the direct host (`db.<ref>.supabase.co`) is IPv6-only; either add the **IPv4 add-on** (keeps direct connections, recommended) or use the pooler host (IPv4, but EF migrations may fail on it).
- **RDS / Aiven:** create the instance, then create the databases.
- Use the **direct** (non-pooler) hostname. EF Core migrations take a session-level advisory lock, which is unreliable through a transaction-mode pooler such as Neon's `-pooler` endpoint or PgBouncer.
- Connection limits are shared by Catalog, Inventory, and Orders (10 each via `Maximum Pool Size=10`) plus Keycloak (capped at 10 via `KC_DB_POOL_MAX_SIZE`). Raise these only after checking the provider's limit.
- If you want stricter TLS, set `PG_SSL_MODE=VerifyFull` (Npgsql) and `PG_JDBC_SSLMODE=verify-full` (Keycloak). `Require` encrypts without validating the certificate.
- Allow the **VPS public IP** on the provider if it enforces an allowlist.

> `Database.Migrate()` runs at startup for Catalog, Inventory, and Orders, so the databases only need to exist and be empty. Keycloak creates its own tables.

### 4.2 MongoDB (Atlas)

1. Create a cluster and a database user.
2. **Network Access → Add IP Address →** the VPS public IP (or `0.0.0.0/0` while testing).
3. Copy the `mongodb+srv://...` connection string and replace `<password>`.
4. URL-encode special characters in the password (`@` → `%40`, `:` → `%3A`, `#` → `%23`, `/` → `%2F`).

`EshopCatalogDb` is created on first write; `MONGO_DATABASE=EshopCatalogDb` selects it.

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

Keycloak uses the same managed PostgreSQL. The `keycloak` database from 4.1 is enough. If your provider uses separate users, set `KEYCLOAK_DB_USER` / `KEYCLOAK_DB_PASSWORD`; otherwise they default to `PG_USER` / `PG_PASSWORD`.

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
- `PG_*`, `CATALOG_DB`, `INVENTORY_DB`, `ORDER_DB`, `KEYCLOAK_DB`
- `MONGO_CONNECTION`, `MONGO_DATABASE`
- `REDIS_CONNECTION`
- `RABBITMQ_CONNECTION`
- `KEYCLOAK_ADMIN_USER`, `KEYCLOAK_ADMIN_PASSWORD`
- UploadCare and Brevo keys

Rules:

- Keep the quotes in the template. `MONGO_CONNECTION`, `REDIS_CONNECTION`, and `RABBITMQ_CONNECTION` contain `&`, `?`, or `,`.
- Do **not** use `$`, `'`, `#`, spaces, `;`, or `=` inside passwords. `$` breaks Compose interpolation; `'` breaks the quoting; `;` breaks Npgsql connection strings.
- Do **not** commit `deploy/.env`.
- If a variable is missing, Compose substitutes an empty string and the container fails at startup — check the deploy logs.

You will paste these values into Dokploy's **Environment** tab in the next step.

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

## 7. Create the Compose service in Dokploy

1. **Projects → Create Project → Create Service → Compose**.
2. **Provider → Git**:
   - Repository: `https://github.com/OualidAissani/Eshop-Backend.git`
   - Branch: `main`
3. **Compose Path**: `deploy/docker-compose.yml`
   - The file uses `context: ..` and `../Src/Services/...` mounts, so it must be deployed from the repository layout. Do not upload a standalone copy of the YAML.
4. **Environment**: paste the full contents of your `deploy/.env`.
5. **Domains** — choose one approach, not both:
   - **Labels (already in the compose file):** nothing to do. `api.<host>` → `gateway:8080`, `auth.<host>` → `keycloak:8080`, with `certresolver=letsencrypt`.
   - **Dokploy UI:** delete the two `labels:` blocks from the compose file, then add in the **Domains** tab:
     - `api.<host>` → service `gateway`, port `8080`, HTTPS on
     - `auth.<host>` → service `keycloak`, port `8080`, HTTPS on
6. Optional: click **Preview Compose** to see how Dokploy injects its labels/network.
7. Click **Deploy** and watch the build logs. The first build produces five .NET images and takes several minutes. Keycloak's first realm import takes 30–90 s.

---

## 8. Verify

### External connectivity

PostgreSQL:

```bash
set -a; source deploy/.env; set +a

docker run --rm -e PGPASSWORD="$PG_PASSWORD" postgres:17-alpine \
  psql "host=$PG_HOST port=$PG_PORT dbname=$CATALOG_DB user=$PG_USER sslmode=${PG_JDBC_SSLMODE}" \
  -c 'select 1'
```

Expected: one row, value `1`.

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

## 9. Keycloak setup

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

## 10. Point Vercel at the VPS

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

## 11. Day-2 operations

### Deploy a new version

Push to `main`, then click **Deploy** in Dokploy (or enable auto-deploy on push in the service settings). A redeploy rebuilds changed images and recreates containers.

### Environment changes

Editing a variable in Dokploy only saves it; **redeploy** for it to reach the containers.

### Rollback

Dokploy keeps the deployment history for the service. Open the previous successful deployment and redeploy it. Database migrations are not automatically reversed — take a logical dump before schema changes.

### Logs

Use the **Logs** tab per service in Dokploy, or the built-in terminal.

### Backups

Neon, Atlas, Upstash, and CloudAMQP run their own backups — confirm the retention window on your plan. For a portable logical snapshot before a risky migration, run this from any machine with Docker and the repo:

```bash
set -a; source deploy/.env; set +a
STAMP=$(date -u +%Y%m%dT%H%M%SZ); mkdir -p backups/$STAMP

docker run --rm -e PGPASSWORD="$PG_PASSWORD" -e PGSSLMODE="$PG_JDBC_SSLMODE" postgres:17-alpine \
  pg_dump -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$CATALOG_DB" \
  --no-owner --no-privileges --clean --if-exists | gzip > backups/$STAMP/catalog.sql.gz

docker run --rm mongo:8 mongodump --uri "$MONGO_CONNECTION" --archive --gzip \
  > backups/$STAMP/mongo.archive.gz
```

Repeat the `pg_dump` line for `INVENTORY_DB`, `ORDER_DB`, and `KEYCLOAK_DB`.

Restore (stop the services in Dokploy first, or scale them to zero):

```bash
gunzip -c backups/<stamp>/catalog.sql.gz | \
  docker run --rm -i -e PGPASSWORD="$PG_PASSWORD" -e PGSSLMODE="$PG_JDBC_SSLMODE" postgres:17-alpine \
  psql -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$CATALOG_DB"
```

### Disk usage

Dokploy keeps images and build cache. Use **Settings → Cleanup** in Dokploy, or on the VPS:

```bash
docker image prune -f
docker builder prune -f --keep-storage 5GB
```

There are no database volumes on the VPS, so clearing Docker data cannot delete your data.

---

## 12. Troubleshooting

| Symptom | Check |
|---|---|
| `dotnet restore` fails during the build | `Src/Services/Directory.Packages.props` must be committed; the Dockerfiles copy it before restore. |
| Dokploy build fails with "context not found" | **Compose Path** must be `deploy/docker-compose.yml` in the repository; the build context is `..` (repo root). |
| Domain returns `404` from Traefik | `gateway`/`keycloak` must be attached to `dokploy-network` (they are in the compose file); redeploy after changing domains; make sure you did not configure the domain both in labels and in the **Domains** tab. |
| Every domain 404s after a Docker upgrade | Dokploy's Traefik must be `v3.6.7` or newer on Docker Engine 28+. Update Dokploy/Traefik. |
| Container exits at startup with an empty setting | A required environment variable is missing in Dokploy; Compose substitutes an empty string. Compare with `deploy/.env.example`. |
| `28P01: password authentication failed` | Password mismatch or unescaped characters. Re-copy the credential; avoid `$ ' # ;`. |
| `too many clients already` / connection limit | Free tiers have low limits. The compose file caps the services at 10 connections each and Keycloak at 10; lower them further or upgrade. Do not switch to a transaction-mode pooler: EF migrations need a session-level lock. |
| `The server does not support SSL connections` | Set `PG_SSL_MODE=Disable` (and `PG_JDBC_SSLMODE=disable`) only for a private/local server. |
| Mongo `Server selection timed out` | Atlas **Network Access** must include the VPS IP; URL-encode the password; check that `+srv` DNS works from the VPS. |
| Redis `NOAUTH` / TLS errors | `password=...` must be in `REDIS_CONNECTION`; managed Redis requires `ssl=True` and usually port `6379`. |
| RabbitMQ `ACCESS_REFUSED` / `NOT_ALLOWED` | The path in the URI is the **virtual host**; port must be `5671` for `amqps`. |
| `502` from the gateway | Check the target service's logs in Dokploy. `/api/Payment` is expected to be `502` because Payment is not deployed. |
| Keycloak restarts or `database "keycloak" does not exist` | Create the `keycloak` database; verify `PG_HOST`, `PG_PORT`, `PG_JDBC_SSLMODE`, and the credentials. |
| Keycloak never becomes healthy | First import is slow. Check its logs; the health port is `9000` (`KC_HEALTH_ENABLED=true`). |
| Keycloak redirects to `http://` or the wrong host | `KC_HOSTNAME` must be `https://auth.<host>`; Traefik must pass `X-Forwarded-Proto`. Redeploy after changing it. |
| CORS error in the browser | `FRONTEND_ORIGIN` must exactly match the origin; redeploy the stack. |
| `401` for every authenticated request | Keycloak issuer/audience mismatch, clock drift, or missing `eshop-api.all` scope. Inspect the token with `jwt.ms`; verify `iss`, `aud`, and `role`. |
| Certificate not issued | DNS must resolve to the VPS, ports 80/443 must be reachable, Cloudflare must be DNS-only for the first issuance. |
| Uploads/emails fail but the API is up | Check `UPLOADCARE_*` and `EMAIL_*` values; the services start without them but external calls fail. |

---

## 13. Security checklist

- Rotate every key that was previously stored in local `appsettings.json`: UploadCare, Brevo, PayPal (if used later), and every database/broker password.
- Keep the Dokploy environment variables private; never commit `deploy/.env`.
- Expose only `22`, `80`, `443` publicly. Restrict `3000` to your IP or reach it over an SSH tunnel.
- On every managed provider, allowlist the **VPS public IP** rather than `0.0.0.0/0`.
- Use strong, unique passwords. URL-encode them in URIs, and avoid `$ ' # ;`.
- Restrict the Keycloak admin console (VPN, IP allowlist, or Traefik basic auth) if it is public.
- Enable the provider's backups/PITR.
- Keep the host and Dokploy patched.
- Add an uptime monitor against `https://api.<host>/health` and `https://auth.<host>/realms/Eshop/.well-known/openid-configuration`.
