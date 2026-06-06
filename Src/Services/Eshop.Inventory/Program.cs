using Eshop.Events;
using Eshop.Inventory.Data;
using Eshop.Inventory.Handler;
using Eshop.Inventory.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.AddServiceDefaults();
}
builder.AddNpgsqlDbContext<InventoryDb>("InventoryDb");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddRedisDistributedCache("redis");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        option.Authority = builder.Configuration["Keycloak:Authority"];
        option.Audience = builder.Configuration["Keycloak:Audience"];
        option.RequireHttpsMetadata = false;
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
        option.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorizationBuilder();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductInventoryQuanityConsumer>();
    x.AddConsumer<ProductStockConsumer>();
    x.AddConsumer<ReductInventoryQuantityFromAnOrderConsumer>();
    x.AddRequestClient<VerifyProductExistence>(new Uri("queue:check-product-existence"));
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));

        cfg.ConfigureEndpoints(context);
        cfg.ReceiveEndpoint("product-inventory-availability", e =>
        {
            e.ConfigureConsumer<ProductInventoryQuanityConsumer>(context);
        });
        cfg.ReceiveEndpoint("product-stock-request", e =>
        {
            e.ConfigureConsumer<ProductStockConsumer>(context);
        });
    });
});
var app = builder.Build();

MigrateDatabase();


app.MapDefaultEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStatusCodePages();
app.UseExceptionHandler(errorApp =>
  {
      errorApp.Run(async context =>
      {
          context.Response.StatusCode = 500;
          await context.Response.WriteAsync("An error occurred.");
      });
  });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

void MigrateDatabase()
{
    var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<InventoryDb>();
    database.Database.Migrate();
}
