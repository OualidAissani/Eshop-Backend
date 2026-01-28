using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var Postgres= builder.AddPostgres("Postgres")
    .WithDataVolume()
    .WithPgAdmin();

var Rabbitmq = builder.AddRabbitMQ("Rabbitmq")
     .WithDataVolume()
     .WithManagementPlugin();

var Redis = builder.AddRedis("Redis")
    .WithDataVolume();
var keycloakPassword = builder.AddParameter("KeycloakPassword", secret: true, value: "admin");
int? keycloakPort = builder.ExecutionContext.IsRunMode ? 8180 : null;
var keycloak = builder.AddKeycloak("keycloak", adminPassword: keycloakPassword, port: keycloakPort)
                      .WithDataVolume()
                      .WithRealmImport("./realms");


var keycloakAuthority = ReferenceExpression.Create(
    $"{keycloak.GetEndpoint("http").Property(EndpointProperty.Url)}/realms/Eshop"
);

var InventoryDb = Postgres.AddDatabase("InventoryDb");
var OrderDb = Postgres.AddDatabase("OrderDb");
var CatalogDb = Postgres.AddDatabase("CatalogDb");

var Catalog=builder.AddProject<Eshop_Catalog>("catalogApi")
   // .WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(CatalogDb)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak);

var Inventory=builder.AddProject<Eshop_Inventory>("inventoryApi")
    //.WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(InventoryDb)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak);

var Order=builder.AddProject<Eshop_Orders>("orderApi")
   // .WithHealthCheck("/health")
    .WithReference(Rabbitmq)
    .WaitFor(Rabbitmq)
    .WithReference(OrderDb)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak);

var Gateway=builder.AddProject<Eshop_Gateway>("Gateway")
   //  .WithHealthCheck("/health")
    .WithReference(Catalog)
    .WithReference(Order)
    .WithReference(Inventory)
    .WithEnvironment("Auth__Authority", keycloakAuthority)
    .WaitFor(keycloak);



builder.Build().Run();
