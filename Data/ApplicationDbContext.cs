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

        var instrumentComparer =
            new ValueComparer<List<InstrumentDefinition>>(
                (left, right) =>
                    JsonSerializer.Serialize(left, jsonOptions) ==
                    JsonSerializer.Serialize(right, jsonOptions),

                instruments =>
                    JsonSerializer.Serialize(
                        instruments,
                        jsonOptions)
                    .GetHashCode(),

                instruments =>
                    JsonSerializer.Deserialize<List<InstrumentDefinition>>(
                        JsonSerializer.Serialize(
                            instruments,
                            jsonOptions),
                        jsonOptions) ?? new List<InstrumentDefinition>());

        builder.Entity<CockpitLayout>(layout =>
        {
            layout.HasKey(item => item.Id);

            layout.HasIndex(item => item.Key)
                .IsUnique();

            layout.Property(item => item.Instruments)
                .HasConversion(
                    instruments =>
                        JsonSerializer.Serialize(
                            instruments,
                            jsonOptions),

                    json =>
                        JsonSerializer.Deserialize<
                            List<InstrumentDefinition>>(
                            json,
                            jsonOptions) ?? new List<InstrumentDefinition>())
                .Metadata.SetValueComparer(instrumentComparer);
        });
    }
}