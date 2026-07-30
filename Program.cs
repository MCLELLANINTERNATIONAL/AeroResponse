using AeroResponse.Components;
using AeroResponse.Components.Account;
using AeroResponse.Data;
using AeroResponse.Data.Mongo;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Data.Mongo.Memberships;
using AeroResponse.Data.Mongo.Payments;
using AeroResponse.Hubs;
using AeroResponse.Repositories;
using AeroResponse.Services;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Razor components and interactive server rendering.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication state services.
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            IdentityConstants.ApplicationScheme;

        options.DefaultSignInScheme =
            IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// SQLite connection used by ASP.NET Core Identity
// and the existing Entity Framework repositories.
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' " +
        "was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseSqlite(connectionString);
    });

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();

// MongoDB settings continue to come from the
// existing MongoDb configuration section.
builder.Services
    .AddOptions<MongoDbSettings>()
    .Bind(
        builder.Configuration.GetSection(
            MongoDbSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.ConnectionString),
        "MongoDb:ConnectionString is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.DatabaseName),
        "MongoDb:DatabaseName is required.")
    .ValidateOnStart();

// MongoClient is thread-safe and should be reused
// for the lifetime of the application.
builder.Services.AddSingleton<IMongoClient>(
    serviceProvider =>
    {
        var settings =
            serviceProvider
                .GetRequiredService<
                    IOptions<MongoDbSettings>>()
                .Value;

        var clientSettings =
            MongoClientSettings
                .FromConnectionString(
                    settings.ConnectionString);

        // Fail MongoDB operations reasonably quickly
        // when the database cannot be reached.
        clientSettings.ServerSelectionTimeout =
            TimeSpan.FromSeconds(5);

        clientSettings.ConnectTimeout =
            TimeSpan.FromSeconds(5);

        return new MongoClient(
            clientSettings);
    });

// Shared MongoDB context.
builder.Services.AddSingleton<MongoDbContext>(
    serviceProvider =>
    {
        var settings =
            serviceProvider
                .GetRequiredService<
                    IOptions<MongoDbSettings>>()
                .Value;

        var client =
            serviceProvider
                .GetRequiredService<IMongoClient>();

        return new MongoDbContext(
            client,
            settings);
    });

// MongoDB connection diagnostics.
builder.Services.AddSingleton<
    MongoConnectionProbe>();

// MongoDB account repository.
//
// Used during registration, navigation name lookup,
// and account-type updates after membership purchase.
builder.Services.AddSingleton<
    MongoUserAccountRepository>();

// MongoDB saved payment-method repository.
//
// Stores a demonstration payment token, card brand,
// last four digits, expiry, cardholder name and
// billing address. It does not store the CVC.
builder.Services.AddSingleton<
    MongoSavedPaymentMethodRepository>();

// MongoDB membership timeline repository.
//
// Stores the selected plan, billing period, account
// type, membership start date and expiry date.
builder.Services.AddSingleton<
    MongoMemberTimelineRepository>();

// ASP.NET Core Identity.
builder.Services
    .AddIdentityCore<ApplicationUser>(
        options =>
        {
            // New accounts can be signed in immediately
            // after successful registration.
            options.SignIn
                .RequireConfirmedAccount = false;

            options.Stores.SchemaVersion =
                IdentitySchemaVersions.Version3;
        })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();

// Existing generic and application repositories.
builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(EfGenericRepository<>));

builder.Services.AddScoped<
    AircraftRepository>();

builder.Services.AddScoped<
    CockpitLayoutRepository>();

builder.Services.AddScoped<
    ScenarioRepository>();

builder.Services.AddScoped<
    MembershipRepository>();

// Existing application services.
builder.Services.AddScoped<
    AircraftService>();

builder.Services.AddScoped<
    CockpitLayoutService>();

builder.Services.AddScoped<
    ScenarioService>();

builder.Services.AddScoped<
    MembershipService>();

builder.Services.AddScoped<
    PerformanceService>();

builder.Services.AddScoped<
    PerformanceDashboardService>();

builder.Services.AddScoped<
    SimulationService>();

builder.Services.AddScoped<
    SimulationSelectionStorage>();

builder.Services.AddScoped<
    SimulationScenarioDataService>();

builder.Services.AddScoped<
    ICockpitLayoutProvider,
    CockpitLayoutProvider>();

builder.Services.AddSingleton<
    SimulationEngine>();

var app = builder.Build();

// Apply Entity Framework migrations and seed the
// initial emergency-scenario data.
await SeedData.InitializeAsync(
    app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

// Identity authentication and authorization
// middleware.
//
// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ASP.NET Core Identity account endpoints,
// including login, logout and account management.
app.MapAdditionalIdentityEndpoints();

// SignalR cockpit simulation hub.
app.MapHub<CockpitHub>(
    "/cockpithub");

// MongoDB health endpoint.
//
// The application can still start even if MongoDB
// is temporarily unavailable. This endpoint tests
// the connection separately.
app.MapGet(
    "/health/mongodb",
    async (
        MongoConnectionProbe probe,
        CancellationToken cancellationToken) =>
    {
        try
        {
            await probe.PingAsync(
                cancellationToken);

            return Results.Ok(
                new
                {
                    status = "healthy",
                    database = "mongodb"
                });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title:
                    "MongoDB connection failed",

                detail:
                    app.Environment.IsDevelopment()
                        ? exception.Message
                        : null,

                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
        }
    });

app.Run();