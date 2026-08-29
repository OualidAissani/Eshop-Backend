using Projects;
using Aspire.Hosting.MongoDB;

var builder = DistributedApplication.CreateBuilder(args);

var Postgres= builder.AddPostgres("Postgres")
    .WithDataVolume()
    .WithPgAdmin();

var Rabbitmq = builder.AddRabbitMQ("Rabbitmq")
     .WithDataVolume()
     .WithManagementPlugin();

var Redis = builder.AddRedis("redis")
    .WithDataVolume();

var Mongo = builder.AddMongoDB("mongo")
    .WithDataVolume();


var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true, value: "admin");

int? keycloakPort = builder.ExecutionContext.IsRunMode ? 8180 : null;
var keycloak = builder.AddKeycloak("keycloak", adminPassword: keycloakPassword, port: keycloakPort)
                    .WithDataVolume()
                      .WithRealmImport("./realms")
                      .WithBindMount("./realms/keycloak-theme.jar", "/opt/keycloak/providers/keycloak-theme.jar");


var keycloakAuthority = ReferenceExpression.Create(
    $"{keycloak.GetEndpoint("http").Property(EndpointProperty.Url)}/realms/Eshop"
);


var InventoryDb = Postgres.AddDatabase("InventoryDb");
var OrderDb = Postgres.AddDatabase("OrderDb");
var CatalogDb = Postgres.AddDatabase("CatalogDb");
var CatalogMongoDb = Mongo.AddDatabase("CatalogMongoDb");
var PaymentDb = Postgres.AddDatabase("PaymentDb");

var Catalog =builder.AddProject<Eshop_Catalog>("catalogApi")
   .WithHttpHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(CatalogDb)
    .WithReference(CatalogMongoDb)
    .WithReference(Redis)
    // Provide Keycloak settings the app expects
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var Inventory=builder.AddProject<Eshop_Inventory>("inventoryApi")
    .WithHttpHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(Redis)
    .WithReference(InventoryDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var Order=builder.AddProject<Eshop_Orders>("orderApi")
   .WithHttpHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(Redis)
    .WithReference(OrderDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WithEnvironment("InventoryBaseUrl", $"{Inventory.GetEndpoint("https").Property(EndpointProperty.Url)}/api/inventory")
    .WaitFor(keycloak);



var Gateway=builder.AddProject<Eshop_Gateway>("Gateway")
   .WithHttpHealthCheck("/health")
    .WithReference(Catalog)
    .WithReference(Order)
    .WithReference(Inventory)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var webFrontend = builder.AddViteApp("webFrontend", "../Eshop.Frontend")
    .WithReference(Gateway)
    .WithEnvironment("VITE_KEYCLOAK_URL",
        keycloak.GetEndpoint("http").Property(EndpointProperty.Url))
    .WithEnvironment("VITE_KEYCLOAK_REALM", "Eshop")
    .WithEnvironment("VITE_KEYCLOAK_CLIENT", "eshop-frontend")
    .WaitFor(Gateway);


builder.Build().Run();
