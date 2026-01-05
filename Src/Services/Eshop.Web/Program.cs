using Eshop.Web.Components;
using Eshop.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Force UTF-8 encoding globally
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register application services (Frontend only)
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<CartService>();
builder.Services.AddSingleton<ToastService>();

// Add localization support
builder.Services.AddLocalization();
builder.Services.AddSwaggerGen();
// Add these service registrations
builder.Services.AddScoped<ITokenStorageService, TokenStorageService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

// Configure HttpClient with the handler
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7194");
})
.AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

builder.Services.AddAuthorizationCore();
var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// Support UTF-8 encoding for requests
app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("ar-SA");
    options.SupportedCultures = new[] { new System.Globalization.CultureInfo("ar-SA") };
    options.SupportedUICultures = new[] { new System.Globalization.CultureInfo("ar-SA") };
});

app.UseAntiforgery();
app.UseHttpsRedirection();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
