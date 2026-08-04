using System.Text.Json;
using System.Text.Json.Serialization;
using AeroResponse.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AeroResponse.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Aircraft> Aircraft { get; set; } = default!;

    public DbSet<CockpitLayout> CockpitLayouts { get; set; } = default!;

    public DbSet<EmergencyScenario> EmergencyScenarios { get; set; } = default!;

    public DbSet<Membership> Memberships { get; set; } = default!;

    public DbSet<PilotProfile> PilotProfiles { get; set; }

    public DbSet<FlightLog> FlightLogs { get; set; } = default!;

    public DbSet<PerformanceResult> PerformanceResults { get; set; } = default!;

    public DbSet<ScenarioRun> ScenarioRuns { get; set; } = default!;

    public DbSet<PilotAction> PilotActions { get; set; } = default!;

    public DbSet<ScenarioProcedureStep> ScenarioProcedureSteps { get; set; } = default!;

    public DbSet<SimulationReport> SimulationReports { get; set; } = default!;

    public DbSet<PilotAchievement> PilotAchievements { get; set; } = default!;

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

        builder.Entity<EmergencyScenario>(scenario =>
        {
            scenario.HasKey(item => item.Id);

            scenario.HasMany(item => item.ProcedureSteps)
                .WithOne(item => item.EmergencyScenario)
                .HasForeignKey(item => item.EmergencyScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ScenarioProcedureStep>(step =>
        {
            step.HasKey(item => item.Id);

            step.Property(item => item.Instruction)
                .IsRequired();

            step.Property(item => item.CorrectAction)
                .IsRequired();

            step.HasIndex(item => new
            {
                item.EmergencyScenarioId,
                item.AircraftType,
                item.StepOrder
            });
        });
        builder.Entity<Aircraft>(entity =>
        {
            entity.OwnsOne(a => a.LandingGearConfig, cfg =>
            {
                cfg.Property(p => p.Kind)
                .HasColumnName("LandingGearKind");

                cfg.OwnsMany(p => p.Units, units =>
                {
                    units.WithOwner().HasForeignKey("AircraftId");
                    units.Property(u => u.Id);
                    units.HasKey(u => u.Id);
                });
            });
        });
    }
}