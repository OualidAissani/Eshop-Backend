using Eshop.Catalog.Data;
using Eshop.Catalog.EventsHandler;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
if(builder.Environment.IsDevelopment())
{
    builder.AddServiceDefaults();
}

builder.Configuration.AddEnvironmentVariables();
// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddHttpClient();

builder.AddNpgsqlDbContext<CatalogDbContext>("CatalogDb");
builder.AddMongoDBClient("CatalogMongoDb");
builder.Services.Configure<MongoSettings>(builder.Configuration.GetRequiredSection("Mongo"));
builder.Services.AddSingleton<MongoCatalogContext>();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddHttpLogging(logging => { });
builder.AddRedisDistributedCache("redis");


Console.WriteLine(
    builder.Configuration.GetConnectionString("CatalogMongoDb"));

Console.WriteLine(
    builder.Configuration.GetConnectionString("redis"));

builder.Services.AddMassTransit(o =>
{
    o.AddConsumer<RetrieveProductPriceConsumer>();
    o.AddConsumer<VerifyProductExistenceConsumer>();

    o.AddEntityFrameworkOutbox<CatalogDbContext>(cfg =>
    {
        cfg.UsePostgres();
        cfg.UseBusOutbox();
    });
    o.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));

        cfg.ReceiveEndpoint("get-product-request", e =>
        {
            e.ConfigureConsumer<RetrieveProductPriceConsumer>(context);
        });
        cfg.ReceiveEndpoint("check-product-existence", e =>
        {
            e.ConfigureConsumer<VerifyProductExistenceConsumer>(context);
        });
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

var app = builder.Build();


app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
app.UseHttpLogging();
app.MapControllers();

app.Run();

void EnsureOutboxDatabase()
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.EnsureCreated();
}

