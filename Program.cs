using AeroResponse.Components;
using AeroResponse.Components.Account;
using AeroResponse.Data;
using AeroResponse.Data.Mongo;
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

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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

// MongoClient is designed to be reused for the lifetime
// of the application, so it is registered as a singleton.
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider
        .GetRequiredService<IOptions<MongoDbSettings>>()
        .Value;

    return new MongoClient(settings.ConnectionString);
});

// Register the application's MongoDB context.
builder.Services.AddSingleton<MongoDbContext>(
    serviceProvider =>
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<MongoDbSettings>>()
            .Value;

        var client = serviceProvider
            .GetRequiredService<IMongoClient>();

        return new MongoDbContext(client, settings);
    });

// Used by the MongoDB health-check endpoint.
builder.Services.AddSingleton<MongoConnectionProbe>();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        options.Stores.SchemaVersion =
            IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();


builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(EfGenericRepository<>));

builder.Services.AddScoped<AircraftRepository>();
builder.Services.AddScoped<CockpitLayoutRepository>();
builder.Services.AddScoped<ScenarioRepository>();
builder.Services.AddScoped<MembershipRepository>();



builder.Services.AddScoped<AircraftService>();
builder.Services.AddScoped<CockpitLayoutService>();
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<PerformanceDashboardService>();
builder.Services.AddScoped<SimulationService>();
builder.Services.AddScoped<SimulationSelectionStorage>();
builder.Services.AddScoped<SimulationScenarioDataService>();
builder.Services.AddScoped<ICockpitLayoutProvider, CockpitLayoutProvider>();
builder.Services.AddSingleton<SimulationEngine>();
builder.Services.AddScoped<AdminDashboardService>();

var app = builder.Build();

// Apply Entity Framework migrations and seed the
// initial emergency-scenario data.
await SeedData.InitializeAsync(app.Services);

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
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ASP.NET Core Identity account endpoints.
app.MapAdditionalIdentityEndpoints();

// SignalR cockpit simulation hub.
app.MapHub<CockpitHub>("/cockpithub");


app.MapGet(
    "/health/mongodb",
    async (
        MongoConnectionProbe probe,
        CancellationToken cancellationToken) =>
    {
        try
        {
            await probe.PingAsync(cancellationToken);

            return Results.Ok(new
            {
                status = "healthy",
                database = "mongodb"
            });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "MongoDB connection failed",
                detail: app.Environment.IsDevelopment()
                    ? exception.Message
                    : null,
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }
    });

app.Run();