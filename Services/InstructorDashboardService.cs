using AeroResponse.Data;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public sealed class InstructorDashboardService
{
    private readonly ApplicationDbContext _context;

    private readonly MongoUserAccountRepository
        _userAccounts;


    public InstructorDashboardService(
        ApplicationDbContext context,
        MongoUserAccountRepository userAccounts)
    {
        _context = context;
        _userAccounts = userAccounts;
    }


    /* =========================================================
       PRODUCTION INSTRUCTOR DASHBOARD
       ========================================================= */

    public async Task<InstructorDashboardVm>
        GetDashboardAsync(
            string currentUserId,
            int days = 30,
            string? selectedPilotUserId = null,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                currentUserId))
        {
            return InstructorDashboardVm
                .Unauthorised();
        }


        /*
         * Find the signed-in AeroResponse account.
         */
        var currentAccount =
            await _userAccounts
                .FindByIdentityUserIdAsync(
                    currentUserId,
                    cancellationToken);


        if (currentAccount is null)
        {
            return InstructorDashboardVm
                .Unauthorised();
        }

        // Administrators inherit trainer-report access and may
        // review system-wide pilot performance.
        if (string.Equals(
                currentAccount.AccountType,
                "admin",
                StringComparison.OrdinalIgnoreCase))
        {
            return await GetSystemWideDashboardAsync(
                days,
                selectedPilotUserId,
                isDevelopmentPreview: false,
                cancellationToken: cancellationToken);
        }


        /*
         * Trainers belong to an owner/company.
         *
         * Company owners can also use this dashboard.
         */
        string? ownerIdentityUserId =
            currentAccount.AccountType switch
            {
                "trainer" =>
                    currentAccount
                        .OwnerIdentityUserId,

                "owner" =>
                    currentAccount
                        .IdentityUserId,

                "owner_small" =>
                    currentAccount
                        .IdentityUserId,

                "owner_large" =>
                    currentAccount
                        .IdentityUserId,

                _ =>
                    null
            };


        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId))
        {
            return InstructorDashboardVm
                .Unauthorised();
        }


        /*
         * Retrieve members belonging to the same company.
         */
        var linkedMembers =
            await _userAccounts
                .FindLinkedMembersAsync(
                    ownerIdentityUserId,
                    cancellationToken);


        /*
         * Instructor Dashboard only reports on pilots.
         *
         * Trainers and other company members are excluded.
         */
        var pilots =
            linkedMembers
                .Where(member =>
                    string.Equals(
                        member.AccountType,
                        "pilot",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(member =>
                    member.DisplayName)
                .Select(member =>
                    new InstructorPilotOptionVm
                    {
                        UserId =
                            member.IdentityUserId,

                        Name =
                            string.IsNullOrWhiteSpace(
                                member.DisplayName)
                                ? "Pilot"
                                : member.DisplayName
                    })
                .ToArray();


        var pilotIds =
            pilots
                .Select(pilot =>
                    pilot.UserId)
                .Where(id =>
                    !string.IsNullOrWhiteSpace(id))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);


        /*
         * A pilot requested through the dropdown must belong
         * to this instructor/company.
         */
        if (!string.IsNullOrWhiteSpace(
                selectedPilotUserId) &&
            !pilotIds.Contains(
                selectedPilotUserId))
        {
            selectedPilotUserId = null;
        }


        var toUtc =
            DateTime.UtcNow;

        DateTime? fromUtc =
            days > 0
                ? toUtc.AddDays(-days)
                : null;


        /*
         * Production query:
         *
         * ONLY reports for pilots connected to this
         * instructor/company.
         */
        var query =
            _context.SimulationReports
                .AsNoTracking()
                .Where(report =>
                    pilotIds.Contains(
                        report.UserId));


        if (!string.IsNullOrWhiteSpace(
                selectedPilotUserId))
        {
            query =
                query.Where(report =>
                    report.UserId ==
                    selectedPilotUserId);
        }


        if (fromUtc.HasValue)
        {
            query =
                query.Where(report =>
                    report.CompletedAt >=
                    fromUtc.Value);
        }


        var reports =
            await query
                .OrderBy(report =>
                    report.CompletedAt)
                .ToListAsync(
                    cancellationToken);


        return BuildDashboard(
            reports,
            pilots,
            ownerIdentityUserId,
            days,
            fromUtc,
            toUtc,
            selectedPilotUserId,
            isDevelopmentPreview: false);
    }


    /* =========================================================
       TEMPORARY DEVELOPMENT PREVIEW

       This method exists only so the dashboard can be viewed
       while authentication is temporarily disabled.

       It intentionally uses all available SimulationReports.

       DO NOT use this method as the final security model.
       ========================================================= */

    public Task<InstructorDashboardVm>
        GetDevelopmentPreviewAsync(
            int days = 30,
            string? selectedPilotUserId = null,
            CancellationToken cancellationToken = default)
    {
        return GetSystemWideDashboardAsync(
            days,
            selectedPilotUserId,
            isDevelopmentPreview: true,
            cancellationToken: cancellationToken);
    }


    private async Task<InstructorDashboardVm>
        GetSystemWideDashboardAsync(
            int days,
            string? selectedPilotUserId,
            bool isDevelopmentPreview,
            CancellationToken cancellationToken = default)
    {
        var toUtc =
            DateTime.UtcNow;

        DateTime? fromUtc =
            days > 0
                ? toUtc.AddDays(-days)
                : null;


        /*
         * Determine available pilots from reports already
         * stored in the relational database.
         */
        var pilotQuery =
            _context.SimulationReports
                .AsNoTracking()
                .Where(report =>
                    !string.IsNullOrWhiteSpace(
                        report.UserId));


        var pilotRecords =
            await pilotQuery
                .Select(report =>
                    new
                    {
                        report.UserId,
                        report.PilotName
                    })
                .ToListAsync(
                    cancellationToken);


        var pilots =
            pilotRecords
                .GroupBy(
                    record =>
                        record.UserId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var pilotName =
                        group
                            .Select(record =>
                                record.PilotName)
                            .FirstOrDefault(name =>
                                !string.IsNullOrWhiteSpace(
                                    name));

                    return new InstructorPilotOptionVm
                    {
                        UserId =
                            group.Key,

                        Name =
                            string.IsNullOrWhiteSpace(
                                pilotName)
                                ? group.Key
                                : pilotName
                    };
                })
                .OrderBy(pilot =>
                    pilot.Name)
                .ToArray();


        var validPilotIds =
            pilots
                .Select(pilot =>
                    pilot.UserId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);


        if (!string.IsNullOrWhiteSpace(
                selectedPilotUserId) &&
            !validPilotIds.Contains(
                selectedPilotUserId))
        {
            selectedPilotUserId = null;
        }


        var query =
            _context.SimulationReports
                .AsNoTracking()
                .AsQueryable();


        if (!string.IsNullOrWhiteSpace(
                selectedPilotUserId))
        {
            query =
                query.Where(report =>
                    report.UserId ==
                    selectedPilotUserId);
        }


        if (fromUtc.HasValue)
        {
            query =
                query.Where(report =>
                    report.CompletedAt >=
                    fromUtc.Value);
        }


        var reports =
            await query
                .OrderBy(report =>
                    report.CompletedAt)
                .ToListAsync(
                    cancellationToken);


        return BuildDashboard(
            reports,
            pilots,
            ownerIdentityUserId:
                isDevelopmentPreview
                    ? "DEVELOPMENT-PREVIEW"
                    : "ADMIN",
            days,
            fromUtc,
            toUtc,
            selectedPilotUserId,
            isDevelopmentPreview);
    }


    /* =========================================================
       DASHBOARD CALCULATION
       ========================================================= */

    private static InstructorDashboardVm
        BuildDashboard(
            IReadOnlyList<SimulationReport> reports,
            IReadOnlyList<InstructorPilotOptionVm> pilots,
            string ownerIdentityUserId,
            int days,
            DateTime? fromUtc,
            DateTime toUtc,
            string? selectedPilotUserId,
            bool isDevelopmentPreview)
    {
        /*
         * Name lookup ensures that the dashboard can show a
         * registered/display pilot name when available.
         */
        var pilotNameLookup =
            pilots
                .Where(pilot =>
                    !string.IsNullOrWhiteSpace(
                        pilot.UserId))
                .GroupBy(
                    pilot =>
                        pilot.UserId,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.First().Name,
                    StringComparer.OrdinalIgnoreCase);


        string ResolvePilotName(
            string userId,
            string reportPilotName)
        {
            if (!string.IsNullOrWhiteSpace(
                    userId) &&
                pilotNameLookup.TryGetValue(
                    userId,
                    out var pilotName) &&
                !string.IsNullOrWhiteSpace(
                    pilotName))
            {
                return pilotName;
            }


            if (!string.IsNullOrWhiteSpace(
                    reportPilotName))
            {
                return reportPilotName;
            }


            return "Unknown Pilot";
        }


        var selectedPilot =
            !string.IsNullOrWhiteSpace(
                selectedPilotUserId)
                ? pilots.FirstOrDefault(
                    pilot =>
                        string.Equals(
                            pilot.UserId,
                            selectedPilotUserId,
                            StringComparison.OrdinalIgnoreCase))
                : null;


        /*
         * Overall totals.
         */
        var passCount =
            reports.Count(report =>
                report.Passed);


        var failCount =
            reports.Count -
            passCount;


        var averageScore =
            Average(
                reports,
                report =>
                    report.OverallScore);


        var averageReactionTime =
            Average(
                reports,
                report =>
                    report.ReactionTimeSeconds);


        var passRate =
            reports.Count == 0
                ? 0
                : (int)Math.Round(
                    passCount *
                    100d /
                    reports.Count);


        /*
         * Daily performance trend.
         */
        var performanceTrend =
            reports
                .GroupBy(report =>
                    report.CompletedAt.Date)
                .Select(group =>
                    new InstructorTrendVm
                    {
                        Date =
                            group.Key,

                        Attempts =
                            group.Count(),

                        AverageScore =
                            (int)Math.Round(
                                group.Average(
                                    report =>
                                        report.OverallScore)),

                        PassRate =
                            group.Count() == 0
                                ? 0
                                : (int)Math.Round(
                                    group.Count(
                                        report =>
                                            report.Passed)
                                    * 100d /
                                    group.Count())
                    })
                .OrderBy(item =>
                    item.Date)
                .ToArray();


        /*
         * Scenario outcomes.
         */
        var scenarioOutcomes =
            reports
                .GroupBy(report =>
                    string.IsNullOrWhiteSpace(
                        report.ScenarioName)
                        ? "Unknown Scenario"
                        : report.ScenarioName)
                .Select(group =>
                    new InstructorScenarioOutcomeVm
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
                                        report.OverallScore)),

                        AverageReactionTime =
                            (int)Math.Round(
                                group.Average(
                                    report =>
                                        report.ReactionTimeSeconds))
                    })
                .OrderByDescending(item =>
                    item.Attempts)
                .ThenBy(item =>
                    item.ScenarioName)
                .ToArray();


        /*
         * Pilot-level performance.
         *
         * If an individual pilot is selected we still have
         * all pilot options in the dropdown, but the table is
         * hidden by the Razor page.
         */
        var pilotPerformance =
            pilots
                .Select(pilot =>
                {
                    var pilotReports =
                        reports
                            .Where(report =>
                                string.Equals(
                                    report.UserId,
                                    pilot.UserId,
                                    StringComparison.OrdinalIgnoreCase))
                            .ToArray();


                    return new InstructorPilotPerformanceVm
                    {
                        PilotUserId =
                            pilot.UserId,

                        PilotName =
                            pilot.Name,

                        Attempts =
                            pilotReports.Length,

                        AverageScore =
                            Average(
                                pilotReports,
                                report =>
                                    report.OverallScore),

                        PassRate =
                            pilotReports.Length == 0
                                ? 0
                                : (int)Math.Round(
                                    pilotReports.Count(
                                        report =>
                                            report.Passed)
                                    * 100d /
                                    pilotReports.Length),

                        AverageReactionTime =
                            Average(
                                pilotReports,
                                report =>
                                    report.ReactionTimeSeconds),

                        LatestAttempt =
                            pilotReports.Length == 0
                                ? null
                                : pilotReports.Max(
                                    report =>
                                        report.CompletedAt)
                    };
                })
                .OrderByDescending(pilot =>
                    pilot.AverageScore)
                .ThenBy(pilot =>
                    pilot.PilotName)
                .ToArray();


        /*
         * Recent simulation activity.
         */
        var recentActivity =
            reports
                .OrderByDescending(report =>
                    report.CompletedAt)
                .Take(10)
                .Select(report =>
                    new InstructorRecentActivityVm
                    {
                        PilotUserId =
                            report.UserId,

                        PilotName =
                            ResolvePilotName(
                                report.UserId,
                                report.PilotName),

                        ScenarioName =
                            string.IsNullOrWhiteSpace(
                                report.ScenarioName)
                                ? "Unknown Scenario"
                                : report.ScenarioName,

                        AircraftName =
                            string.IsNullOrWhiteSpace(
                                report.AircraftName)
                                ? "Unknown Aircraft"
                                : report.AircraftName,

                        CompletedAt =
                            report.CompletedAt,

                        OverallScore =
                            report.OverallScore,

                        Passed =
                            report.Passed
                    })
                .ToArray();


        /*
         * Training-area averages.
         *
         * These give instructors a macro view of the
         * areas where future coaching should be focused.
         */
        var scoreBreakdown =
            new InstructorScoreBreakdownVm
            {
                ProcedureAccuracy =
                    Average(
                        reports,
                        report =>
                            report.ProcedureAccuracyScore),

                DecisionMaking =
                    Average(
                        reports,
                        report =>
                            report.DecisionMakingScore),

                ChecklistUsage =
                    Average(
                        reports,
                        report =>
                            report.ChecklistUsageScore),

                TimeManagement =
                    Average(
                        reports,
                        report =>
                            report.TimeManagementScore),

                Communication =
                    Average(
                        reports,
                        report =>
                            report.CommunicationScore)
            };


        var activePilots =
            reports
                .Where(report =>
                    !string.IsNullOrWhiteSpace(
                        report.UserId))
                .Select(report =>
                    report.UserId)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .Count();


        return new InstructorDashboardVm
        {
            IsAuthorised =
                true,

            IsDevelopmentPreview =
                isDevelopmentPreview,

            OwnerIdentityUserId =
                ownerIdentityUserId,

            SelectedDays =
                days,

            FromUtc =
                fromUtc,

            ToUtc =
                toUtc,

            SelectedPilotUserId =
                selectedPilotUserId,

            SelectedPilotName =
                selectedPilot?.Name,

            Pilots =
                pilots,

            TotalPilots =
                pilots.Count,

            ActivePilots =
                activePilots,

            TotalAttempts =
                reports.Count,

            AverageScore =
                averageScore,

            PassCount =
                passCount,

            FailCount =
                failCount,

            PassRate =
                passRate,

            AverageReactionTime =
                averageReactionTime,

            SafetyCriticalErrors =
                reports.Sum(report =>
                    report.SafetyCriticalErrors),

            PerformanceTrend =
                performanceTrend,

            ScenarioOutcomes =
                scenarioOutcomes,

            PilotPerformance =
                pilotPerformance,

            RecentActivity =
                recentActivity,

            ScoreBreakdown =
                scoreBreakdown
        };
    }


    /* =========================================================
       HELPER
       ========================================================= */

    private static int Average(
        IEnumerable<SimulationReport> reports,
        Func<SimulationReport, int> selector)
    {
        var values =
            reports
                .Select(selector)
                .ToArray();


        return values.Length == 0
            ? 0
            : (int)Math.Round(
                values.Average());
    }
}


/* =============================================================
   VIEW MODELS
   ============================================================= */

public sealed class InstructorDashboardVm
{
    public bool IsAuthorised { get; init; }

    public bool IsDevelopmentPreview { get; init; }

    public string OwnerIdentityUserId { get; init; } =
        string.Empty;

    public int SelectedDays { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime ToUtc { get; init; }

    public string? SelectedPilotUserId { get; init; }

    public string? SelectedPilotName { get; init; }

    public IReadOnlyList<InstructorPilotOptionVm>
        Pilots { get; init; } = [];

    public int TotalPilots { get; init; }

    public int ActivePilots { get; init; }

    public int TotalAttempts { get; init; }

    public int AverageScore { get; init; }

    public int PassCount { get; init; }

    public int FailCount { get; init; }

    public int PassRate { get; init; }

    public int AverageReactionTime { get; init; }

    public int SafetyCriticalErrors { get; init; }

    public IReadOnlyList<InstructorTrendVm>
        PerformanceTrend { get; init; } = [];

    public IReadOnlyList<InstructorScenarioOutcomeVm>
        ScenarioOutcomes { get; init; } = [];

    public IReadOnlyList<InstructorPilotPerformanceVm>
        PilotPerformance { get; init; } = [];

    public IReadOnlyList<InstructorRecentActivityVm>
        RecentActivity { get; init; } = [];

    public InstructorScoreBreakdownVm
        ScoreBreakdown { get; init; } =
            new();


    public static InstructorDashboardVm
        Unauthorised()
    {
        return new InstructorDashboardVm
        {
            IsAuthorised = false,
            IsDevelopmentPreview = false
        };
    }
}


public sealed class InstructorPilotOptionVm
{
    public string UserId { get; init; } =
        string.Empty;

    public string Name { get; init; } =
        string.Empty;
}


public sealed class InstructorTrendVm
{
    public DateTime Date { get; init; }

    public int Attempts { get; init; }

    public int AverageScore { get; init; }

    public int PassRate { get; init; }
}


public sealed class InstructorScenarioOutcomeVm
{
    public string ScenarioName { get; init; } =
        string.Empty;

    public int Attempts { get; init; }

    public int Passes { get; init; }

    public int Failures =>
        Attempts - Passes;

    public int AverageScore { get; init; }

    public int AverageReactionTime { get; init; }

    public int PassRate =>
        Attempts == 0
            ? 0
            : (int)Math.Round(
                Passes *
                100d /
                Attempts);
}


public sealed class InstructorPilotPerformanceVm
{
    public string PilotUserId { get; init; } =
        string.Empty;

    public string PilotName { get; init; } =
        string.Empty;

    public int Attempts { get; init; }

    public int AverageScore { get; init; }

    public int PassRate { get; init; }

    public int AverageReactionTime { get; init; }

    public DateTime? LatestAttempt { get; init; }
}


public sealed class InstructorRecentActivityVm
{
    public string PilotUserId { get; init; } =
        string.Empty;

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


public sealed class InstructorScoreBreakdownVm
{
    public int ProcedureAccuracy { get; init; }

    public int DecisionMaking { get; init; }

    public int ChecklistUsage { get; init; }

    public int TimeManagement { get; init; }

    public int Communication { get; init; }
}