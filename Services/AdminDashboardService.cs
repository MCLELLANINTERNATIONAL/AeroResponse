using AeroResponse.Data;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Data.Mongo.Reports;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public sealed class AdminDashboardService(
    ApplicationDbContext context,
    MongoPilotReportRepository pilotReportRepository,
    MongoUserAccountRepository userAccountRepository)
{
    public async Task<AdminDashboardVm> GetDashboardAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var toUtc = DateTime.UtcNow;

        DateTime? fromUtc =
            days > 0
                ? toUtc.AddDays(-days)
                : null;

        // Training/reporting data lives in MongoDB. Keep the SQL context only
        // for the scenario catalogue, which is still managed by EF Core.
        var reports = await pilotReportRepository.GetReportsAsync(
            fromUtc,
            toUtc,
            cancellationToken);

        var totalRegisteredUsers = checked(
            (int)await userAccountRepository.CountAllAsync(
                cancellationToken));

        var totalScenarios =
            await context.EmergencyScenarios
                .AsNoTracking()
                .CountAsync(cancellationToken);

        var activeScenarios =
            await context.EmergencyScenarios
                .AsNoTracking()
                .CountAsync(
                    scenario => scenario.IsActive,
                    cancellationToken);

        var passCount =
            reports.Count(report => report.Passed);

        var failCount =
            reports.Count - passCount;

        var scenarioPopularity = reports
            .GroupBy(report =>
                string.IsNullOrWhiteSpace(
                    report.ScenarioName)
                    ? "Unknown scenario"
                    : report.ScenarioName)
            .Select(group =>
                new ScenarioSummaryVm
                {
                    ScenarioName = group.Key,

                    Attempts = group.Count(),

                    Passes = group.Count(
                        report => report.Passed),

                    AverageScore =
                        (int)Math.Round(
                            group.Average(
                                report =>
                                    report.OverallScore))
                })
            .OrderByDescending(
                item => item.Attempts)
            .ThenBy(
                item => item.ScenarioName)
            .ToList();

        var usageByDay = reports
            .GroupBy(
                report =>
                    report.CompletedAt.Date)
            .Select(group =>
                new DailyUsageVm
                {
                    Date = group.Key,

                    Attempts = group.Count(),

                    UniquePilots = group
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
            .OrderBy(item => item.Date)
            .ToList();

        var recentActivity = reports
            .Take(8)
            .Select(report =>
                new RecentTrainingVm
                {
                    PilotName =
                        string.IsNullOrWhiteSpace(
                            report.PilotName)
                            ? "Unknown pilot"
                            : report.PilotName,

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
                })
            .ToList();

        return new AdminDashboardVm
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            SelectedDays = days,

            TotalRegisteredUsers =
                totalRegisteredUsers,

            ActivePilots = reports
                .Select(
                    report => report.UserId)
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
                        passCount * 100d /
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
}

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
        ScenarioPopularity
    { get; init; } = [];

    public IReadOnlyList<DailyUsageVm>
        UsageByDay
    { get; init; } = [];

    public IReadOnlyList<RecentTrainingVm>
        RecentActivity
    { get; init; } = [];
}

public sealed class ScenarioSummaryVm
{
    public string ScenarioName { get; init; }
        = string.Empty;

    public int Attempts { get; init; }

    public int Passes { get; init; }

    public int Failures =>
        Attempts - Passes;

    public int AverageScore { get; init; }

    public int PassRate =>
        Attempts == 0
            ? 0
            : (int)Math.Round(
                Passes * 100d /
                Attempts);
}

public sealed class DailyUsageVm
{
    public DateTime Date { get; init; }

    public int Attempts { get; init; }

    public int UniquePilots { get; init; }
}

public sealed class RecentTrainingVm
{
    public string PilotName { get; init; }
        = string.Empty;

    public string ScenarioName { get; init; }
        = string.Empty;

    public string AircraftName { get; init; }
        = string.Empty;

    public DateTime CompletedAt { get; init; }

    public int OverallScore { get; init; }

    public bool Passed { get; init; }
}