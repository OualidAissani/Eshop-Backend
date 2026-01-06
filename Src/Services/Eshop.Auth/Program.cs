using Eshop.Auth.Data;
using Eshop.Auth.Models;
using Eshop.Auth.Services;
using Eshop.Auth.Services.IServices;
using MicroservicesTest.AuthenticationApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var JwtSetting=builder.Configuration.GetSection("JwtSettings").Get<JWTSettings>();
JwtSetting.Validate();
builder.Services.AddDbContext<AuthDbContext>(option => option.UseNpgsql(builder.Configuration.GetConnectionString("AuthDBString")));
builder.Services.AddIdentity<AppUser, Roles>(o =>
{
    o.Password.RequiredLength = 8;
    o.Password.RequireDigit = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Password.RequiredUniqueChars = 0;
    o.Password.RequireLowercase = false;
}).AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>(sp =>
    new JwtTokenService(
        JwtSetting.SecretKey,
        JwtSetting.Issuer,
        JwtSetting.Audience,
        JwtSetting.Algorithm,
        JwtSetting.AccessTokenExpiryMinutes
        )
);
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
