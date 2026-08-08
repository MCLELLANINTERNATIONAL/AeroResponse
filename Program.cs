using AeroResponse.Components;
using AeroResponse.Components.Account;
using AeroResponse.Data;
using AeroResponse.Data.Mongo;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Data.Mongo.Memberships;
using AeroResponse.Data.Mongo.Payments;
using AeroResponse.Data.Mongo.Referrals;
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
        policy => policy.Requirements.Add(
            new AccountPermissionRequirement(
                AccountPermissions.PilotPages)));

    options.AddPolicy(
        AccountPermissions.TrainerReports,
        policy => policy.Requirements.Add(
            new AccountPermissionRequirement(
                AccountPermissions.TrainerReports)));

    options.AddPolicy(
        AccountPermissions.AdminPages,
        policy => policy.Requirements.Add(
            new AccountPermissionRequirement(
                AccountPermissions.AdminPages)));
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AccountPermissionService>();
builder.Services.AddScoped<PilotReportAccessService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AccountPermissionHandler>();

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

// SQLite is used by ASP.NET Core Identity and the
// Entity Framework application repositories.
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlite(connectionString));

builder.Services
    .AddDatabaseDeveloperPageExceptionFilter();

// =========================================================
// MONGODB SETTINGS
// =========================================================

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

// MongoClient is thread-safe and should be reused for
// the lifetime of the application.
builder.Services.AddSingleton<IMongoClient>(
    serviceProvider =>
    {
        var settings =
            serviceProvider
                .GetRequiredService<
                    IOptions<MongoDbSettings>>()
                .Value;

        var clientSettings =
            MongoClientSettings.FromConnectionString(
                settings.ConnectionString);

        // Prevent MongoDB connection attempts from
        // waiting indefinitely when MongoDB is unavailable.
        clientSettings.ServerSelectionTimeout =
            TimeSpan.FromSeconds(5);

        clientSettings.ConnectTimeout =
            TimeSpan.FromSeconds(5);

        return new MongoClient(clientSettings);
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
builder.Services.AddSingleton<
    MongoUserAccountRepository>();

// MongoDB saved payment-method repository.
builder.Services.AddSingleton<
    MongoSavedPaymentMethodRepository>();

// MongoDB membership timeline repository.
builder.Services.AddSingleton<
    MongoMemberTimelineRepository>();

// MongoDB owner referral-code repository.
builder.Services.AddSingleton<
    MongoOwnerReferralCodeRepository>();

// =========================================================
// ASP.NET CORE IDENTITY
// =========================================================

builder.Services
    .AddIdentityCore<ApplicationUser>(
        options =>
        {
            // Registered users can sign in immediately
            // without confirming an email address.
            options.SignIn.RequireConfirmedAccount =
                false;

            options.Stores.SchemaVersion =
                IdentitySchemaVersions.Version3;
        })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();

// =========================================================
// REPOSITORIES
// =========================================================

// Generic Entity Framework repository.
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

// Provides dashboards for trainers and company owners.
// Results are restricted to pilots linked to their company.
builder.Services.AddScoped<
    InstructorDashboardService>();

// Provides the system-wide Administration Dashboard.
// Access is restricted by the admin permission policy.
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

// Creates, rotates and resolves owner invitation codes.
builder.Services.AddSingleton<
    OwnerReferralCodeService>();

// =========================================================
// BUILD APPLICATION
// =========================================================

// Create required MongoDB indexes.
var app = builder.Build();

// Apply Entity Framework migrations and seed the
// initial application data.
await SeedData.InitializeAsync(
    app.Services);

// Create MongoDB indexes and initialise the existing
// company-member counters.
using (var scope = app.Services.CreateScope())
{
    var userAccountRepository =
        scope.ServiceProvider
            .GetRequiredService<
                MongoUserAccountRepository>();

    var referralCodeRepository =
        scope.ServiceProvider
            .GetRequiredService<
                MongoOwnerReferralCodeRepository>();

    await userAccountRepository
        .EnsureIndexesAsync();

    await referralCodeRepository
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

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// =========================================================
// APPLICATION ENDPOINTS
// =========================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ASP.NET Core Identity endpoints, including login,
// logout, registration and account management.
app.MapAdditionalIdentityEndpoints();

// SignalR cockpit simulation hub.
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
                    status = "healthy",
                    database = "mongodb"
                });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                detail: app.Environment.IsDevelopment()
                    ? exception.Message
                    : null,
                statusCode: StatusCodes
                    .Status503ServiceUnavailable,
                title: "MongoDB connection failed");
        }
    });

app.Run();