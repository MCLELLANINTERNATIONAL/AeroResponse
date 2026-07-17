using AeroResponse.Components;
using AeroResponse.Components.Account;
using AeroResponse.Data;
using AeroResponse.Hubs;
using AeroResponse.Repositories;
using AeroResponse.Services;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor components and interactive server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication state services.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Database configuration.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ASP.NET Core Identity.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<
    IEmailSender<ApplicationUser>,
    IdentityNoOpEmailSender>();

// Reusable generic CRUD repository.
builder.Services.AddScoped(
    typeof(IGenericRepository<>),
    typeof(EfGenericRepository<>));

// Entity-specific repositories.
builder.Services.AddScoped<AircraftRepository>();
builder.Services.AddScoped<ScenarioRepository>();
builder.Services.AddScoped<MembershipRepository>();

// Application services.
builder.Services.AddScoped<AircraftService>();
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<SimulationService>();
builder.Services.AddScoped<SimulationSelectionStorage>();
builder.Services.AddSingleton<ICockpitLayoutProvider, CockpitLayoutProvider>();
builder.Services.AddSingleton<ICockpitLayoutProvider, CockpitLayoutProvider>();
builder.Services.AddSingleton<SimulationEngine>();

var app = builder.Build();

// Apply migrations and add the initial emergency scenarios.
await SeedData.InitializeAsync(app.Services);

// HTTP request pipeline.
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

// Identity account endpoints.
app.MapAdditionalIdentityEndpoints();

// SignalR cockpit simulation hub.
app.MapHub<CockpitHub>("/cockpithub");

app.Run();