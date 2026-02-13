using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var Postgres= builder.AddPostgres("Postgres")
    .WithDataVolume()
    .WithPgAdmin();

var Rabbitmq = builder.AddRabbitMQ("Rabbitmq")
     .WithDataVolume()
     .WithManagementPlugin();

var Redis = builder.AddRedis("redis")
    .WithDataVolume();


var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true, value: "admin");

int? keycloakPort = builder.ExecutionContext.IsRunMode ? 8180 : null;
var keycloak = builder.AddKeycloak("keycloak", adminPassword: keycloakPassword, port: keycloakPort)
                      .WithDataVolume()
                      .WithRealmImport("./realms");


var keycloakAuthority = ReferenceExpression.Create(
    $"{keycloak.GetEndpoint("http").Property(EndpointProperty.Url)}/realms/Eshop"
);

                                                         ///KEYCLAOCK NOT WORKING FIX IT NP
var InventoryDb = Postgres.AddDatabase("InventoryDb");
var OrderDb = Postgres.AddDatabase("OrderDb");
var CatalogDb = Postgres.AddDatabase("CatalogDb");
var PayementDb = Postgres.AddDatabase("PayementDb");

var Catalog =builder.AddProject<Eshop_Catalog>("catalogApi")
   // .WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(CatalogDb)
    .WithReference(Redis)
    // Provide Keycloak settings the app expects
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var Inventory=builder.AddProject<Eshop_Inventory>("inventoryApi")
    //.WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(Redis)
    .WithReference(InventoryDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var Order=builder.AddProject<Eshop_Orders>("orderApi")
   // .WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(Redis)
    .WithReference(OrderDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WithEnvironment("InventoryBaseUrl", $"{Inventory.GetEndpoint("https").Property(EndpointProperty.Url)}/api/inventory")
    .WaitFor(keycloak);

var Payement=builder.AddProject<Eshop_Payment>("paymentApi")
   // .WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(PayementDb)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

var Gateway=builder.AddProject<Eshop_Gateway>("Gateway")
   //  .WithHealthCheck("/health")
    .WithReference(Catalog)
    .WithReference(Order)
    .WithReference(Inventory)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", "eshop-api")
    .WaitFor(keycloak);

builder.Build().Run();
