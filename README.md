# Eshop Backend

[![CI](https://github.com/OualidAissani/Eshop-Backend/actions/workflows/ci.yml/badge.svg)](https://github.com/OualidAissani/Eshop-Backend/actions/workflows/ci.yml)

A microservices-based e-commerce backend built with .NET 10. Services are independently deployable, each owning its own PostgreSQL database. Inter-service communication is event-driven via RabbitMQ and MassTransit. All external traffic is routed through a YARP API gateway that enforces JWT authentication at the edge.

---
### Services

| Service         | Responsibility                                                          |
|-----------------|-------------------------------------------------------------------------|
| Catalog         | Product and category management, image uploads via UploadCare           |
| Inventory       | Stock tracking and availability, Redis-cached reads                     |
| Orders          | Order creation and lifecycle management via saga state machine          |
| Payment         | PayPal payment processing                                               |
| Gateway         | YARP reverse proxy — routes traffic, enforces JWT at the edge           |
| Web             | Blazor Server frontend                                                  |
| Events          | Shared event and message contracts across services                      |
| ServiceDefaults | Common config for service discovery, health checks, and OpenTelemetry   |

---

## Key Technical Decisions

**Order Saga (Distributed Transaction)**
Order processing is coordinated by a MassTransit saga state machine persisted with Entity Framework Core. The saga manages the full order lifecycle across services: the order is submitted, inventory is reserved, and the order is either confirmed or compensated if reservation fails. This avoids distributed transactions while keeping consistency guarantees.

**Database-per-Service**
Each service owns its own PostgreSQL database with no shared schema. Services only communicate via events, not direct DB access.

**Authentication at the Edge**
Keycloak issues JWT tokens. The Gateway validates them via YARP before any request reaches a downstream service — services themselves trust the forwarded identity without re-validating.

**Idempotency**
All mutating REST endpoints enforce idempotency keys to prevent duplicate submissions under concurrent or retry scenarios.

**Cursor-based Pagination**
List endpoints use cursor-based pagination instead of offset pagination for stable, performant results under concurrent writes.

---

## Tech Stack

| Layer            | Technology                                      |
|------------------|-------------------------------------------------|
| Language         | C# (.NET 10)                                    |
| Database         | PostgreSQL (per service) + Entity Framework Core|
| Messaging        | RabbitMQ + MassTransit                          |
| Cache            | Redis                                           |
| Auth             | Keycloak (OAuth2 / OpenID Connect / JWT)        |
| Gateway          | YARP Reverse Proxy                              |
| Frontend         | Blazor Server                                   |
| Orchestration    | .NET Aspire                                     |
| Observability    | OpenTelemetry (metrics + tracing)               |
| Containerization | Docker                                          |
| Image Storage    | UploadCare                                      |
| Payments         | PayPal                                          |

---

## Getting Started

**Prerequisites:** Docker, .NET 10 SDK

1. Clone the repository

```bash
git clone https://github.com/OualidAissani/Eshop-Backend.git
cd Eshop-Backend
```

2. Configure external service credentials in `appsettings.json` for the relevant services:

```json
// Catalog Service
"UploadCare": {
  "PublicKey": "<your-uploadcare-public-key>",
  "SecretKey": "<your-uploadcare-secret-key>",
  "Store": "1"
},
"GatewayUrl": "https://localhost:7194"

// Payment Service
"Paypal": {
  "WebhookId": "<webhook-id>",
  "ClientId": "<client-id>",
  "SecretKey": "<secret-key>"
}
```

3. Run all services via .NET Aspire:

```bash
dotnet run --project Src/Services/Eshop.AppHost
```

Aspire handles service discovery, RabbitMQ, Redis, and PostgreSQL startup automatically.
