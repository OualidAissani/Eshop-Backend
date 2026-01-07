using Eshop.Catalog.Data;
using Eshop.Catalog.EventsHandler;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services;
using Eshop.Catalog.Services.IServices;
using Eshop.Events;
using MassTransit;
using MicroservicesTest.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
// Add services to the container.
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JWTSettings>();
jwtSettings.Validate();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddHttpClient();

builder.Services.AddDbContext<CatalogDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration.GetConnectionString("CatalogDb"));
});

builder.Services.AddOpenApi();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();


builder.Services.AddMassTransit(o =>
{
    o.AddConsumer<RetrieveProductPriceConsumer>();
    o.AddConsumer<VerifyProductExistenceConsumer>();

    o.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
        cfg.ReceiveEndpoint("get-product-request", e =>
        {
            e.ConfigureConsumer<RetrieveProductPriceConsumer>(context);
        });
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        option.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidAudience = jwtSettings.Audience,
            ValidIssuer = jwtSettings.Issuer,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5),
            RequireExpirationTime = true
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
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
