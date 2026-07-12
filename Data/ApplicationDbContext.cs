using AeroResponse.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Aircraft> Aircraft { get; set; }

    public DbSet<EmergencyScenario> EmergencyScenarios { get; set; }

    public DbSet<Membership> Memberships { get; set; }

    public DbSet<PilotProfile> PilotProfiles { get; set; }

    public DbSet<FlightLog> FlightLogs { get; set; }

    public DbSet<PerformanceResult> PerformanceResults { get; set; }

    public DbSet<ScenarioRun> ScenarioRuns { get; set; }

    public DbSet<PilotAction> PilotActions { get; set; }

    public DbSet<ScenarioProcedureStep> ScenarioProcedureSteps { get; set; }

    public DbSet<SimulationReport> SimulationReports { get; set; }
}