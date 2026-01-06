
# Eshop

An e-commerce backend built with microservices. Still working on it.
What's this?
Basic online shop split into separate services - auth, products, inventory, and orders. Each service has its own database and they talk to each other through RabbitMQ.
Services

Auth - handles login/register with JWT tokens
Catalog - manages products and categories, uploads images to UploadCare
Inventory - tracks stock levels
Orders - creates and manages orders
Gateway - routes requests to the right service
Events - shared message contracts

Built with

.NET 10
PostgreSQL
RabbitMQ + MassTransit
JWT auth
Docker

Running it
You'll need databases for each service, RabbitMQ running, and appsettings.json files with your connection strings. Then just dotnet run each service.
Check the Swagger docs at the ports in launchSettings.json.
Status
Mostly works but incomplete. Some endpoints are stubbed out, web frontend isn't done yet.

## Feedback

If you have any feedback, please reach out to me at walidaissani200418@gmail.com

