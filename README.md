# Eshop Backend

[![CI](https://github.com/OualidAissani/Eshop-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/OualidAissani/Eshop-Backend/actions/workflows/ci.yml)

A microservices e-commerce backend for the Algerian market, built with .NET 10. Services are independently deployable and communicate through events (RabbitMQ + MassTransit). All external traffic goes through a YARP API gateway that validates Keycloak JWTs and applies rate limiting at the edge.

**Scope decisions**

- **Guest checkout only.** Orders are tracked by order number + phone number.
- **Cash on delivery only.** Orders with any other payment method are rejected at the API level. There is no payment service.
- **Delivery pricing by wilaya.** Each of the 58 wilayas has its own delivery price, editable by admins.

---

## Services

| Service | Responsibility | Data stores |
|---|---|---|
| **Catalog** | Products, categories, collections, per-product discounts, image uploads (UploadCare) | MongoDB, PostgreSQL, Redis |
| **Inventory** | Stock tracking and atomic reservation | PostgreSQL, Redis |
| **Orders** | Order creation, order lifecycle saga, delivery prices, order tracking | PostgreSQL, Redis |
| **Notification** | Consumes events and sends transactional emails (Brevo) | Redis |
| **Gateway** | YARP reverse proxy: JWT validation, CORS, rate limiting | none |
| **Events** | Shared event and message contracts | none |
| **ServiceDefaults** | Shared service discovery, health checks, OpenTelemetry | none |
| **AppHost** | .NET Aspire orchestration for local development | none |
| **Test** | xUnit test project | none |

---

## Key Technical Decisions

- **Order saga + transactional outbox:** a MassTransit saga coordinates order submission and inventory reservation, with compensation if reservation fails. The EF Core outbox makes the order write and event publish atomic.
- **Atomic stock updates:** reservation is a single conditional `ExecuteUpdateAsync` (`WHERE quantity >= requested`), so concurrent checkouts cannot oversell.
- **Idempotency keys (Redis):** order creation requires an `x-Idempotency-Key` header, so retries and double submits do not create duplicate orders.
- **Auth at the edge:** the gateway validates Keycloak JWTs and applies sliding-window rate limiting before traffic reaches any service.
- **Discounts resolved at read time:** stored per product in MongoDB and applied on read, so cached catalog data never holds a discount price.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language / runtime | C# (.NET 10), ASP.NET Core |
| Data access | Entity Framework Core (PostgreSQL), MongoDB.Driver |
| Databases | PostgreSQL, MongoDB |
| Messaging | RabbitMQ + MassTransit |
| Cache | Redis |
| Auth | Keycloak (OAuth2 / OpenID Connect / JWT) |
| Gateway | YARP |
| Orchestration | .NET Aspire |
| Observability | OpenTelemetry (via ServiceDefaults) |
| Media | UploadCare |
| Email | Brevo |
| Testing | xUnit, FluentAssertions, Imposter |
| CI | GitHub Actions |
| Deployment | Docker, Dokploy |

---

## Getting Started

**Prerequisites:** Docker, .NET 10 SDK

1. Clone the repository

```bash
git clone https://github.com/OualidAissani/Eshop-Backend.git
cd Eshop-Backend
```

2. Configure credentials for the services that need them (see `deploy/.env.example` for the full list of variable names):

```json
// Catalog service: image uploads
"UploadCare": {
  "PublicKey": "<your-uploadcare-public-key>",
  "SecretKey": "<your-uploadcare-secret-key>",
  "Store": "1"
}
```

The Notification service needs an email API key (`EMAIL_API_KEY` in deployment).

3. Run everything through .NET Aspire:

```bash
dotnet run --project Src/Services/Eshop.AppHost
```

Aspire starts PostgreSQL, MongoDB, Redis, RabbitMQ and Keycloak, wires up service discovery, and launches all services.

---

## Testing

```bash
dotnet test Src/Services/Eshop.slnx
```

The suite covers the order saga, order service, inventory consumer, notification service, media service, and product and category services. Some tests connect to MongoDB at `mongodb://localhost:27017`, so start one first:

```bash
docker run -d --name mongodb -p 27017:27017 mongo:7.0
```

CI (GitHub Actions) restores, builds, scans for vulnerable packages, starts MongoDB, and runs the tests with coverage on every push and pull request to `main`.

---

## Production Deployment

The deployment targets **Dokploy** on a VPS, as **two independent Compose services**:

| Dokploy service | Contents | Source |
|---|---|---|
| `eshop` | catalog, inventory, orders, notification, gateway | this repo: `deploy/docker-compose.yml` |
| `keycloak` | Keycloak with the login theme and realm export baked in | separate private repo |

PostgreSQL, MongoDB, Redis and RabbitMQ are managed services hosted outside the VPS, wired in through environment variables. Dokploy's Traefik provides TLS for `api.<host>` (gateway) and `auth.<host>` (Keycloak). The frontend is deployed separately on Vercel.

- `deploy/docker-compose.yml`: the Eshop stack
- `deploy/.env.example`: environment variables for the **Eshop** service

See **[DEPLOYMENT.md](DEPLOYMENT.md)** for the step-by-step guide: managed services, environment variables, the Keycloak service, Dokploy setup, domain routing and troubleshooting.
