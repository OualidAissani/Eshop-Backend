using Eshop.Notification.EventHandler;
using Eshop.Notification.Services;
using Eshop.Notification.Services.IServices;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


if (builder.Environment.IsDevelopment())
{
    builder.AddServiceDefaults();
}


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services.AddHttpClient("EmailService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EmailService:BaseUrl"]);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("api-key", builder.Configuration["EmailService:ApiKey"]);
});

builder.Services.AddMassTransit(o =>
{
    o.AddConsumer<SendEmailEventConsumer>();
    o.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));
        cfg.ConfigureEndpoints(context);

    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Audience = builder.Configuration["Keycloak:Audience"];
        o.Authority = builder.Configuration["Keycloak:Authority"];

        o.RequireHttpsMetadata = builder.Environment.IsProduction() ? true : false;

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };

        o.Events = new JwtBearerEvents
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
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDefaultEndpoints();

}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
