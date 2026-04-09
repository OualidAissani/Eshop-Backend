using Eshop.Payment.Data;
using Eshop.Payment.EventHandler;
using Eshop.Payment.Services;
using Eshop.Payment.Services.IServices;
using Hangfire;
using Hangfire.PostgreSql;
using MassTransit;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PaymentDbContext>("PaymentDb");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHangfire(x => 
    x.UsePostgreSqlStorage(options => 
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("PayementDb"));
    })
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings());



builder.Services.AddHangfireServer();

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Audience = builder.Configuration["Keycloak:Audience"];
    o.Authority = builder.Configuration["Keycloak:Authority"];

    o.RequireHttpsMetadata = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

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
builder.Services.AddAuthorizationBuilder();

builder.Services.AddAntiforgery();

builder.Services.AddHttpClient();


builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CreatePaymentConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("Rabbitmq"));
        cfg.ConfigureEndpoints(context);
    });
});




var app = builder.Build();
MigrateDatabase();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHangfireDashboard();//auth for pd

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
void MigrateDatabase()
{
    var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    database.Database.Migrate();
}
public partial class Program { }
