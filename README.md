# Eshop Backend (In-Progress)

A microservices-based e-commerce backend built with .NET 10, featuring independent services for product catalog management, inventory tracking, and order processing with a Blazor web frontend.

## Architecture

The system uses a microservices pattern with service-to-service communication via RabbitMQ and MassTransit. Authentication is managed through Keycloak with JWT tokens:

| Service | Purpose |
|---------|---------|
| Catalog | Manages products and categories, uploads images to UploadCare |
| Inventory | Tracks stock levels and product availability with Redis caching |
| Orders | Creates and manages customer orders |
| Gateway | API gateway with YARP reverse proxy routing to services |
| Web | Blazor Server frontend application |
| Events | Shared event and message contracts |
| ServiceDefaults | Common configuration for service discovery, health checks, and OpenTelemetry |

## Tech Stack

- Language: C# (.NET 10)
- Database: PostgreSQL (separate database per service)
- Message Broker: RabbitMQ with MassTransit
- Cache: Redis
- Authentication: Keycloak with JWT
- Frontend: Blazor Server
- Reverse Proxy: YARP
- Service Orchestration: .NET Aspire
- Observability: OpenTelemetry with metrics and tracing
- Containerization: Docker

## Prerequisites

- .NET 10 SDK
- PostgreSQL
- RabbitMQ
- Redis
- Keycloak
- Docker 

## Getting Started

1. Clone the repository
2. Configure the required values in the `appsettings.json` files for each service:
   - **Catalog Service**:
     - `UploadCare` (PublicKey, SecretKey, Store)
     - `GatewayUrl`
   - **Order Service**:
     - `InventoryBaseUrl`
   Example configuration:

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
  "WebhookId": "",
  "ClientId": "",
  "SecretKey": ""
}
 ```

3. Start all services using the AppHost project:
```bash
dotnet run --project Src/Services/Eshop.AppHost
