using AeroResponse.Data;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public sealed class AdminDashboardService(
    ApplicationDbContext context)
{
    public async Task<AdminDashboardVm> GetDashboardAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var toUtc =
            DateTime.UtcNow;

        DateTime? fromUtc =
            days > 0
                ? toUtc.AddDays(-days)
                : null;

        // =========================================================
        // LOAD SIMULATION REPORTS
        // =========================================================

        var reportQuery =
            context.SimulationReports
                .AsNoTracking()
                .AsQueryable();

        if (fromUtc.HasValue)
        {
            reportQuery =
                reportQuery.Where(
                    report =>
                        report.CompletedAt >=
                        fromUtc.Value);
        }

        var reports =
            await reportQuery
                .OrderByDescending(
                    report =>
                        report.CompletedAt)
                .ToListAsync(
                    cancellationToken);

        // =========================================================
        // RESOLVE REGISTERED PILOT NAMES
        // =========================================================
        //
        // Historical SimulationReports may contain an email address
        // in PilotName.
        //
        // The UserId on each report is the reliable link back to the
        // registered ASP.NET Identity ApplicationUser.
        //
        // Therefore the Admin Dashboard displays ApplicationUser.FullName
        // instead of relying on the old PilotName saved in the report.
        // =========================================================

        var reportUserIds =
            reports
                .Select(report =>
                    report.UserId)
                .Where(userId =>
                    !string.IsNullOrWhiteSpace(
                        userId))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        var registeredUsers =
            await context.Users
                .AsNoTracking()
                .Where(user =>
                    reportUserIds.Contains(
                        user.Id))
                .ToListAsync(
                    cancellationToken);

        var pilotNameLookup =
            registeredUsers
                .ToDictionary(
                    user =>
                        user.Id,

                    user =>
                        !string.IsNullOrWhiteSpace(
                            user.FullName)
                            ? user.FullName
                            : !string.IsNullOrWhiteSpace(
                                user.UserName)
                                ? user.UserName
                                : "Unknown pilot",

                    StringComparer.OrdinalIgnoreCase);

        // =========================================================
        // PLATFORM TOTALS
        // =========================================================

        var totalRegisteredUsers =
            await context.Users
                .AsNoTracking()
                .CountAsync(
                    cancellationToken);

        var totalScenarios =
            await context.EmergencyScenarios
                .AsNoTracking()
                .CountAsync(
                    cancellationToken);

        var activeScenarios =
            await context.EmergencyScenarios
                .AsNoTracking()
                .CountAsync(
                    scenario =>
                        scenario.IsActive,
                    cancellationToken);

        var passCount =
            reports.Count(
                report =>
                    report.Passed);

        var failCount =
            reports.Count -
            passCount;

        // =========================================================
        // SCENARIO POPULARITY / EFFECTIVENESS
        // =========================================================

        var scenarioPopularity =
            reports
                .GroupBy(
                    report =>
                        string.IsNullOrWhiteSpace(
                            report.ScenarioName)
                            ? "Unknown scenario"
                            : report.ScenarioName)
                .Select(
                    group =>
                        new ScenarioSummaryVm
                        {
                            ScenarioName =
                                group.Key,

                            Attempts =
                                group.Count(),

                            Passes =
                                group.Count(
                                    report =>
                                        report.Passed),

                            AverageScore =
                                (int)Math.Round(
                                    group.Average(
                                        report =>
                                            report.OverallScore))
                        })
                .OrderByDescending(
                    item =>
                        item.Attempts)
                .ThenBy(
                    item =>
                        item.ScenarioName)
                .ToList();

        // =========================================================
        // DAILY PLATFORM USAGE
        // =========================================================

        var usageByDay =
            reports
                .GroupBy(
                    report =>
                        report.CompletedAt.Date)
                .Select(
                    group =>
                        new DailyUsageVm
                        {
                            Date =
                                group.Key,

                            Attempts =
                                group.Count(),

                            UniquePilots =
                                group
                                    .Select(
                                        report =>
                                            report.UserId)
                                    .Where(
                                        userId =>
                                            !string.IsNullOrWhiteSpace(
                                                userId))
                                    .Distinct()
                                    .Count()
                        })
                .OrderBy(
                    item =>
                        item.Date)
                .ToList();

        // =========================================================
        // RECENT PLATFORM ACTIVITY
        // =========================================================

        var recentActivity =
            reports
                .Take(8)
                .Select(
                    report =>
                    {
                        var resolvedPilotName =
                            pilotNameLookup.TryGetValue(
                                report.UserId,
                                out var registeredPilotName)
                                ? registeredPilotName
                                : ResolveFallbackPilotName(
                                    report.PilotName);

                        return new RecentTrainingVm
                        {
                            PilotName =
                                resolvedPilotName,

                            ScenarioName =
                                report.ScenarioName,

                            AircraftName =
                                report.AircraftName,

                            CompletedAt =
                                report.CompletedAt,

                            OverallScore =
                                report.OverallScore,

                            Passed =
                                report.Passed
                        };
                    })
                .ToList();

        // =========================================================
        // BUILD DASHBOARD
        // =========================================================

        return new AdminDashboardVm
        {
            FromUtc =
                fromUtc,

            ToUtc =
                toUtc,

            SelectedDays =
                days,

            TotalRegisteredUsers =
                totalRegisteredUsers,

            ActivePilots =
                reports
                    .Select(
                        report =>
                            report.UserId)
                    .Where(
                        userId =>
                            !string.IsNullOrWhiteSpace(
                                userId))
                    .Distinct()
                    .Count(),

            TotalAttempts =
                reports.Count,

            AverageScore =
                reports.Count == 0
                    ? 0
                    : (int)Math.Round(
                        reports.Average(
                            report =>
                                report.OverallScore)),

            PassCount =
                passCount,

            FailCount =
                failCount,

            PassRate =
                reports.Count == 0
                    ? 0
                    : (int)Math.Round(
                        passCount *
                        100d /
                        reports.Count),

            TotalScenarios =
                totalScenarios,

            ActiveScenarios =
                activeScenarios,

            ScenarioPopularity =
                scenarioPopularity,

            UsageByDay =
                usageByDay,

            RecentActivity =
                recentActivity
        };
    }

    // =============================================================
    // PILOT NAME FALLBACK
    // =============================================================
    //
    // Normally the registered ApplicationUser.FullName is used.
    //
    // If an old report no longer has a matching Identity user,
    // avoid deliberately displaying an email address in the
    // Admin Dashboard.
    // =============================================================

    private static string ResolveFallbackPilotName(
        string? storedPilotName)
    {
        if (string.IsNullOrWhiteSpace(
                storedPilotName))
        {
            return "Unknown pilot";
        }

        if (storedPilotName.Contains(
                '@',
                StringComparison.Ordinal))
        {
            return "Unknown pilot";
        }

        return storedPilotName.Trim();
    }
}


// =================================================================
// ADMIN DASHBOARD VIEW MODEL
// =================================================================

public sealed class AdminDashboardVm
{
    public DateTime? FromUtc { get; init; }

    public DateTime ToUtc { get; init; }

    public int SelectedDays { get; init; }

    public int TotalRegisteredUsers { get; init; }

    public int ActivePilots { get; init; }

    public int TotalAttempts { get; init; }

    public int AverageScore { get; init; }

    public int PassCount { get; init; }

    public int FailCount { get; init; }

    public int PassRate { get; init; }

    public int TotalScenarios { get; init; }

    public int ActiveScenarios { get; init; }

    public IReadOnlyList<ScenarioSummaryVm>
        ScenarioPopularity { get; init; } = [];

    public IReadOnlyList<DailyUsageVm>
        UsageByDay { get; init; } = [];

    public IReadOnlyList<RecentTrainingVm>
        RecentActivity { get; init; } = [];
}


// =================================================================
// SCENARIO SUMMARY VIEW MODEL
// =================================================================

public sealed class ScenarioSummaryVm
{
    public string ScenarioName { get; init; } =
        string.Empty;

    public int Attempts { get; init; }

    public int Passes { get; init; }

    public int Failures =>
        Attempts -
        Passes;

    public int AverageScore { get; init; }

    public int PassRate =>
        Attempts == 0
            ? 0
            : (int)Math.Round(
                Passes *
                100d /
                Attempts);
}


// =================================================================
// DAILY USAGE VIEW MODEL
// =================================================================

public sealed class DailyUsageVm
{
    public DateTime Date { get; init; }

    public int Attempts { get; init; }

    public int UniquePilots { get; init; }
}


// =================================================================
// RECENT TRAINING VIEW MODEL
// =================================================================

public sealed class RecentTrainingVm
{
    public string PilotName { get; init; } =
        string.Empty;

    public string ScenarioName { get; init; } =
        string.Empty;

    public string AircraftName { get; init; } =
        string.Empty;

    public DateTime CompletedAt { get; init; }

    public int OverallScore { get; init; }

    public bool Passed { get; init; }
}