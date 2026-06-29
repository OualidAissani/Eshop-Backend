using Eshop.Events;
using Eshop.Orders.Data;
using Eshop.Orders.EventHandler;
using Eshop.Orders.Sagas;
using Eshop.Orders.Services;
using Eshop.Orders.Services.IServices;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.AddServiceDefaults();
}
builder.AddNpgsqlDbContext<OrderDbContext>("OrderDb");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi();

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<ICartService, CartService>();

builder.AddRedisDistributedCache("redis");

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMassTransit(o =>
{

    o.AddSagaStateMachine<OrderStateMachineSaga, OrderState>()
   .EntityFrameworkRepository(r =>
   {
       r.ExistingDbContext<OrderDbContext>();
       r.UsePostgres();
   });
    o.AddEntityFrameworkOutbox<OrderDbContext>(cfg =>
    {
        cfg.UsePostgres();
        cfg.UseBusOutbox();
    });
    o.AddConsumer<OrderConfirmedConsumer>();
    o.AddConsumer<UpdateCartProductConsumer>();
    o.AddConsumer<OrderCompensateConsumer>();
    o.AddRequestClient<GetProductRequest>(new Uri("queue:retrieve-product-price"));
    o.AddRequestClient<ProductInventoryAvailibityForOrderRequest>(new Uri("queue:product-inventory-quanity"));
    o.AddRequestClient<ProductStockRequest>(new Uri("queue:product-stock"));
    o.AddRequestClient<CreatePaymentRecordRequest>(new Uri("queue:create-payment"));
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
    var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    database.Database.Migrate();
}
