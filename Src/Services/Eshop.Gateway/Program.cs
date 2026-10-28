using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

var proxy = builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

if (builder.Environment.IsDevelopment())
    proxy.AddServiceDiscoveryDestinationResolver();

var rateLimiting = builder.Configuration.GetSection("RateLimiting");
var rateLimitingEnabled = rateLimiting.GetValue("Enabled", true);
var globalPermitLimit = rateLimiting.GetValue("GlobalPermitLimit", 600);
var orderCreationPermitLimit = rateLimiting.GetValue("OrderCreationPermitLimit", 20);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = (context, _) =>
    {
        var http = context.HttpContext;
        http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiter").LogWarning(
            "429 {Method} {Path} remote={Remote} xff={Xff}",
            http.Request.Method, http.Request.Path, http.Connection.RemoteIpAddress,
            http.Request.Headers["X-Forwarded-For"].ToString());
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        return ValueTask.CompletedTask;
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        rateLimitingEnabled
            ? RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = globalPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                })
            : RateLimitPartition.GetNoLimiter("rate-limiting-off"));

    options.AddPolicy("order-creation", httpContext =>
        rateLimitingEnabled
            ? RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = orderCreationPermitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 4,
                    QueueLimit = 0
                })
            : RateLimitPartition.GetNoLimiter("rate-limiting-off"));
});


var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://maktabahislamiia.vercel.app"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
       .AllowAnyHeader()
       .AllowAnyMethod()
       .WithExposedHeaders("Token-Expired");
    });
});



builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(option =>
    {
        option.Authority = builder.Configuration["Keycloak:Authority"];
        option.Audience = builder.Configuration["Keycloak:Audience"];
        option.RequireHttpsMetadata = builder.Configuration.GetValue(
            "Keycloak:RequireHttpsMetadata",
            builder.Environment.IsProduction());
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
builder.Services.AddAuthorization();
var app = builder.Build();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors("AllowFrontend");

var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Number of proxy hops to trust when resolving the client IP (used by rate limiting).
    // 1 = only the edge proxy in front of the gateway (Traefik/Dokploy).
    // 2 = Vercel's proxy + the edge proxy (browser -> Vercel rewrite -> VPS).
    ForwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1
};

var trustedProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
foreach (var trustedProxy in trustedProxies)
{
    if (IPAddress.TryParse(trustedProxy, out var address))
    {
        forwardedHeaders.KnownProxies.Add(address);
    }
}

if (builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAll"))
{
    forwardedHeaders.KnownNetworks.Clear();
    forwardedHeaders.KnownProxies.Clear();
}

app.UseForwardedHeaders(forwardedHeaders);

app.UseRateLimiter();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapControllers();

app.Run();
