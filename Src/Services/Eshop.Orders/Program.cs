using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.EventHandler;
using Eshop.Orders.Services;
using Eshop.Orders.Services.IServices;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Refit;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<OrderDbContext>("OrderDb");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
var inventoryApiUrl = builder.Configuration["InventoryBaseUrl"];
builder.Services.AddRefitClient<IUpdateInventory>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(inventoryApiUrl));

builder.Services.AddMassTransit(o =>
{
       o.AddConsumer<OrderProductsEvent>();

       // Add request clients with the target queue addresses
       o.AddRequestClient<GetProductRequest>(new Uri("queue:get-product-request"));
       o.AddRequestClient<ProductInventoryAvailibityForOrderRequest>(new Uri("queue:product-inventory-availability"));

       o.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));


        cfg.ConfigureEndpoints(context);
    });

});
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
var app = builder.Build();
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
MigrateDatabase();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
void MigrateDatabase()
{
    var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    database.Database.Migrate();
}
