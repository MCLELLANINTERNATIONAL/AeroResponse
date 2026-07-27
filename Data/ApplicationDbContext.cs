using AeroResponse.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json.Serialization;

namespace AeroResponse.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<CockpitLayout> CockpitLayouts { get; set; }

    public DbSet<EmergencyScenario> EmergencyScenarios { get; set; }

    public DbSet<Membership> Memberships { get; set; }

    public DbSet<PilotProfile> PilotProfiles { get; set; }

    public DbSet<FlightLog> FlightLogs { get; set; }

    public DbSet<PerformanceResult> PerformanceResults { get; set; }

    public DbSet<ScenarioRun> ScenarioRuns { get; set; }

    public DbSet<PilotAction> PilotActions { get; set; }

    public DbSet<ScenarioProcedureStep> ScenarioProcedureSteps { get; set; }

    public DbSet<SimulationReport> SimulationReports { get; set; }

    public DbSet<PilotAchievement> PilotAchievements { get; set; }

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        var detailsComparer =
            new ValueComparer<CockpitLayoutDetails>(
                (left, right) =>
                    JsonSerializer.Serialize(left, jsonOptions) ==
                    JsonSerializer.Serialize(right, jsonOptions),

                details =>
                    JsonSerializer.Serialize(
                        details,
                        jsonOptions)
                    .GetHashCode(),

                details =>
                    JsonSerializer.Deserialize<CockpitLayoutDetails>(
                        JsonSerializer.Serialize(
                            details,
                            jsonOptions),
                        jsonOptions) ?? new CockpitLayoutDetails());

        builder.Entity<CockpitLayout>(layout =>
        {
            layout.HasKey(item => item.Id);

            layout.HasIndex(item => item.Key)
                .IsUnique();

            layout.Property(item => item.Details)
                .HasConversion(
                    details =>
                        JsonSerializer.Serialize(
                            details,
                            jsonOptions),

                    json =>
                        JsonSerializer.Deserialize<CockpitLayoutDetails>(
                            json,
                            jsonOptions) ?? new CockpitLayoutDetails())
                .Metadata.SetValueComparer(detailsComparer);
        });
    }
}