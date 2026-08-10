using AeroResponse.Components;
using AeroResponse.Components.Account;
using AeroResponse.Data;
using AeroResponse.Data.Mongo;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Data.Mongo.Memberships;
using AeroResponse.Data.Mongo.Payments;
using AeroResponse.Data.Mongo.Referrals;
using AeroResponse.Data.Mongo.Reports;
using AeroResponse.Hubs;
using AeroResponse.Repositories;
using AeroResponse.Services;
using AeroResponse.Services.Authorization;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Controls;
using AeroResponse.Simulation.Layouts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// RAZOR COMPONENTS
// =========================================================

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });

// =========================================================
// AUTHENTICATION AND AUTHORIZATION
// =========================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AccountPermissions.PilotPages,
        policy =>
            policy.Requirements.Add(
                new AccountPermissionRequirement(
                    AccountPermissions.PilotPages)));

    options.AddPolicy(
        AccountPermissions.TrainerReports,
        policy =>
            policy.Requirements.Add(
                new AccountPermissionRequirement(
                    AccountPermissions.TrainerReports)));

    options.AddPolicy(
        AccountPermissions.AdminPages,
        policy =>
            policy.Requirements.Add(
                new AccountPermissionRequirement(
                    AccountPermissions.AdminPages)));
});

builder.Services
    .AddCascadingAuthenticationState();

builder.Services.AddScoped<
    IdentityRedirectManager>();

builder.Services.AddScoped<
    AccountPermissionService>();

builder.Services.AddScoped<
    PilotReportAccessService>();

builder.Services.AddScoped<
    AircraftAccessService>();

builder.Services.AddScoped<
    Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    AccountPermissionHandler>();

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

// =========================================================
// SQLITE AND ENTITY FRAMEWORK
// =========================================================

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services
    .AddDbContext<ApplicationDbContext>(
        options =>
            options.UseSqlite(
                connectionString));

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();

// =========================================================
// MONGODB SETTINGS
// =========================================================

builder.Services
    .AddOptions<MongoDbSettings>()
    .Bind(
        builder.Configuration
            .GetSection(
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

        clientSettings.ServerSelectionTimeout =
            TimeSpan.FromSeconds(5);

        clientSettings.ConnectTimeout =
            TimeSpan.FromSeconds(5);

        return new MongoClient(
            clientSettings);
    });

builder.Services.AddSingleton<
    MongoDbContext>(
        serviceProvider =>
        {
            var settings =
                serviceProvider
                    .GetRequiredService<
                        IOptions<MongoDbSettings>>()
                    .Value;

            var client =
                serviceProvider
                    .GetRequiredService<
                        IMongoClient>();

            return new MongoDbContext(
                client,
                settings);
        });

builder.Services.AddSingleton<
    MongoConnectionProbe>();

builder.Services.AddSingleton<
    MongoUserAccountRepository>();

builder.Services.AddSingleton<
    MongoSavedPaymentMethodRepository>();

builder.Services.AddSingleton<
    MongoMemberTimelineRepository>();

builder.Services.AddSingleton<
    MongoOwnerReferralCodeRepository>();

builder.Services.AddSingleton<
    MongoPilotReportRepository>();

// =========================================================
// ASP.NET CORE IDENTITY
// =========================================================

builder.Services
    .AddIdentityCore<ApplicationUser>(
        options =>
        {
            options.SignIn
                .RequireConfirmedAccount =
                false;

            options.Stores.SchemaVersion =
                IdentitySchemaVersions.Version3;
        })
    .AddEntityFrameworkStores<
        ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();

// =========================================================
// REPOSITORIES
// =========================================================

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

// =========================================================
// APPLICATION SERVICES
// =========================================================

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
    PerformanceScoringEngine>();

builder.Services.AddScoped<
    InstructorDashboardService>();

builder.Services.AddScoped<
    AdminDashboardService>();

builder.Services.AddScoped<
    SimulationService>();

builder.Services.AddScoped<
    SimulationSelectionStorage>();

builder.Services.AddScoped<
    SimulationScenarioDataService>();

builder.Services.AddScoped<
    ScenarioTriggerEvaluator>();

builder.Services.AddScoped<
    ICockpitLayoutProvider,
    CockpitLayoutProvider>();

builder.Services.AddSingleton<
    SimulationEngine>();

builder.Services.AddSingleton<
    CockpitControlCatalog>();

builder.Services.AddSingleton<
    VoiceCommandParser>();

builder.Services.AddScoped<
    ICockpitControlHandler,
    StandardCockpitControlHandler>();

builder.Services.AddScoped<
    CockpitCommandService>();

builder.Services.AddScoped<
    AiInstructorService>();

builder.Services.AddSingleton<
    OwnerReferralCodeService>();

// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();

await SeedData.InitializeAsync(
    app.Services);

// =========================================================
// MONGODB INITIALISATION
// =========================================================

using (var scope =
       app.Services.CreateScope())
{
    var userAccountRepository =
        scope.ServiceProvider
            .GetRequiredService<
                MongoUserAccountRepository>();

    var referralCodeRepository =
        scope.ServiceProvider
            .GetRequiredService<
                MongoOwnerReferralCodeRepository>();

    var pilotReportRepository =
        scope.ServiceProvider
            .GetRequiredService<
                MongoPilotReportRepository>();

    await userAccountRepository
        .EnsureIndexesAsync();

    await referralCodeRepository
        .EnsureIndexesAsync();

    await pilotReportRepository
        .EnsureIndexesAsync();

    await userAccountRepository
        .SynchronizeAllOwnerMemberCountsAsync();
}

// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler(
        errorHandlingPath: "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    pathFormat: "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// =========================================================
// APPLICATION ENDPOINTS
// =========================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<CockpitHub>(
    "/cockpithub");

// =========================================================
// MONGODB HEALTH ENDPOINT
// =========================================================

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
                    status =
                        "healthy",

                    database =
                        "mongodb"
                });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                detail:
                    app.Environment
                        .IsDevelopment()
                        ? exception.Message
                        : null,

                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable,

                title:
                    "MongoDB connection failed");
        }
    });

app.Run();