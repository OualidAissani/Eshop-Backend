using Eshop.Inventory.Data;
using MicroservicesTest.AuthenticationApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwtSetting = builder.Configuration.GetSection("JwtSettings").Get<JWTSettings>();
builder.Services.AddDbContext<InventoryDb>(o =>
{
    o.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDb"));
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var key = Encoding.UTF8.GetBytes(jwtSetting.SecretKey);
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSetting.Issuer,
            ValidAudience = jwtSetting.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromSeconds(5),
            RequireExpirationTime = true,
        };
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = c =>
            {
                Console.WriteLine("OnAuthenticationFailed: " + c.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = c =>
            {
                Console.WriteLine("OnTokenValidated: " + c.SecurityToken);
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
