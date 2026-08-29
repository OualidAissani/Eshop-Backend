using Eshop.Catalog.Data;
using Eshop.Catalog.EventsHandler;
using Eshop.Catalog.Services;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scrutor;
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
builder.Services.Decorate<IProductService, CachedProductService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<IDiscountService, DiscountService>();
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
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));

        cfg.ConfigureEndpoints(context);
        cfg.ReceiveEndpoint("retrieve-product-price", e =>
        {
            e.ConfigureConsumer<RetrieveProductPriceConsumer>(context);
        });
        cfg.ReceiveEndpoint("verify-product-existence", e =>
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
        option.RequireHttpsMetadata = builder.Environment.IsProduction() ? true : false;
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
        var exception = context.Features.Get<IExceptionHandlerFeature>();

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = exception?.Error.Message, // Remove in production if you don't want to expose details
            Instance = context.Request.Path
        };

        context.Response.StatusCode = problem.Status.Value;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
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

