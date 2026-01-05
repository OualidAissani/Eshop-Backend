using Eshop.Orders.Data;
using Eshop.Orders.EventHandler;
using MassTransit;
using MicroservicesTest.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JWTSettings>();
builder.Services.AddDbContext<OrderDbContext>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb"));
    
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();


builder.Services.AddMassTransit(o =>
{
       o.AddConsumer<OrderProductsEvent>();
       o.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
    });

});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
        o.TokenValidationParameters=new TokenValidationParameters
        {
            ValidateAudience=true,
            ValidateIssuer=true,
            ValidateLifetime=true,
            ValidateIssuerSigningKey=true,
            ValidIssuer=jwtSettings.Issuer,
            ValidAudience=jwtSettings.Audience,
            IssuerSigningKey=new SymmetricSecurityKey(key),
            ClockSkew=TimeSpan.FromSeconds(5),
            RequireExpirationTime=true,
        };
        o.Events=new JwtBearerEvents
        {
            OnAuthenticationFailed=c=>
            {
                Console.WriteLine("OnAuthenticationFailed: "+c.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated=c=>
            {
                Console.WriteLine("OnTokenValidated: "+c.SecurityToken);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
