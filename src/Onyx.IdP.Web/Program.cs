using Onyx.IdP.Core;
using Onyx.IdP.Infrastructure;
using Onyx.IdP.Infrastructure.Data;
using OpenIddict.Abstractions;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCoreServices();
builder.Services.AddInfrastructureServices(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Configure Email Settings
builder.Services.Configure<Onyx.IdP.Core.Settings.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Register RazorViewToStringRenderer
builder.Services.AddTransient<Onyx.IdP.Web.Services.IRazorViewToStringRenderer, Onyx.IdP.Web.Services.RazorViewToStringRenderer>();


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Features";
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Features/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/Features/Admin/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/Features/Shared/{0}.cshtml");
    options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
});

// OPENIDDICT CONFIGURATION
// =============================================================================
builder.Services.AddOpenIddict()
    // A. Core: Integrate with EF Core to store tokens/apps in DB
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();

        // Enable Quartz.NET integration.
        options.UseQuartz();
    })
    // B. Server: Handle the OIDC Protocol
    .AddServer(options =>
    {
        // 1. Define the endpoints (matches ConnectController routes)
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetEndSessionEndpointUris("/connect/logout");

        // 2. Define flows
        options.AllowAuthorizationCodeFlow()
               .AllowClientCredentialsFlow()
               .AllowRefreshTokenFlow();

        // 3. Define scopes
        options.RegisterScopes(
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess,
            "api");

        // 4. Security (Dev only: Ephemeral keys)
        // IN PRODUCTION: Use .AddEncryptionCertificate() and .AddSigningCertificate()
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate()
               .DisableAccessTokenEncryption();

        // 5. ASP.NET Core Integration
        options.UseAspNetCore()
               // We enable "Passthrough" so our ConnectController handles the logic
               // instead of OpenIddict handling it automatically invisibly.
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();

        // Disable Transport Security Requirement for Development/Container
        // if (builder.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        // {
        //     options.UseAspNetCore().DisableTransportSecurityRequirement();
        // }
        // 6. Dynamic Scopes
        // We disable scope validation so that we can add new scopes to the database
        // without needing to register them in code (Program.cs).
        //options.DisableScopeValidation();
    })
    // C. Validation: Needed if this app also consumes tokens (e.g. UserInfo endpoint)
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// QUARTZ CONFIGURATION
// =============================================================================
builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET hosted service.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var app = builder.Build();

// PIPELINE
// =============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
