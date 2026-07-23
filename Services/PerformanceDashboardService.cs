using AeroResponse.Data;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public sealed class PerformanceDashboardService(ApplicationDbContext context)
{
    public async Task<PerformanceDashboardVm> GetDashboardAsync(string userId)
    {
        var reports = await context.SimulationReports.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var achievements = await context.PilotAchievements.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.EarnedAt)
            .ToListAsync();

        var latest = reports.LastOrDefault();
        var recent = reports.TakeLast(10).ToList();
        var average = reports.Count == 0 ? 0 : (int)Math.Round(reports.Average(x => x.OverallScore));
        var best = reports.Count == 0 ? 0 : reports.Max(x => x.OverallScore);
        var improvement = recent.Count < 2 ? 0 : recent[^1].OverallScore - recent[0].OverallScore;

        return new PerformanceDashboardVm
        {
            Latest = latest,
            Reports = reports.OrderByDescending(x => x.CreatedAt).ToList(),
            ChartReports = recent,
            Achievements = achievements,
            AverageScore = average,
            BestScore = best,
            Improvement = improvement
        };
    }

    public async Task<SimulationReport> SaveCompletedPracticeAsync(SimulationReport report)
    {
        report.CreatedAt = DateTime.UtcNow;
        report.AiFeedback = GenerateAiStyleFeedback(report);
        context.SimulationReports.Add(report);
        await context.SaveChangesAsync();
        await UnlockAchievementsAsync(report);
        PerformanceFeed.Notify(report.UserId);
        return report;
    }

    private async Task UnlockAchievementsAsync(SimulationReport report)
    {
        var candidates = new List<PilotAchievement>();
        if (report.ReactionTimeSeconds <= 10) candidates.Add(New("quick-thinker", "Quick Thinker", "Responded to an emergency within ten seconds.", "⚡"));
        if (report.ChecklistAccuracyScore >= 90) candidates.Add(New("checklist-master", "Checklist Master", "Achieved at least 90% procedural accuracy.", "✓"));
        if (report.DecisionMakingScore >= 90) candidates.Add(New("calm-pressure", "Calm Under Pressure", "Achieved at least 90% decision-making performance.", "✈"));
        if (report.SafetyCriticalErrors == 0) candidates.Add(New("protocol-pro", "Protocol Pro", "Completed a scenario without a safety-critical error.", "⚓"));
        if (report.CommunicationScore >= 90) candidates.Add(New("communication-star", "Communication Star", "Demonstrated excellent emergency communication.", "★"));
        if (report.OverallScore == 100) candidates.Add(New("perfect-score", "Perfect Score", "Achieved 100% in a training scenario.", "🏆"));

        var existing = await context.PilotAchievements.Where(x => x.UserId == report.UserId).Select(x => x.Code).ToListAsync();
        foreach (var achievement in candidates.Where(x => !existing.Contains(x.Code)))
        {
            achievement.UserId = report.UserId;
            context.PilotAchievements.Add(achievement);
        }
        await context.SaveChangesAsync();
    }

    private static PilotAchievement New(string code, string name, string description, string icon) => new()
    { Code = code, Name = name, Description = description, Icon = icon, EarnedAt = DateTime.UtcNow };

    public static string GenerateAiStyleFeedback(SimulationReport report)
    {
        var strengths = new List<string>();
        var improvements = new List<string>();
        if (report.ChecklistAccuracyScore >= 85) strengths.Add("strong procedural discipline"); else improvements.Add("review the checklist sequence");
        if (report.DecisionMakingScore >= 85) strengths.Add("sound decision-making under pressure"); else improvements.Add("pause briefly to confirm safety-critical decisions");
        if (report.ReactionTimeSeconds <= 12) strengths.Add("a prompt initial response"); else improvements.Add("identify and respond to the primary warning faster");
        if (report.CommunicationScore < 85) improvements.Add("declare the emergency and communicate intentions earlier");

        var lead = strengths.Count > 0
            ? $"You demonstrated {string.Join(" and ", strengths)}."
            : "This attempt provides a useful baseline for improvement.";
        var next = improvements.Count > 0
            ? $"For the next attempt, {string.Join(", and ", improvements)}."
            : "Continue practising at a higher difficulty while maintaining the same disciplined response.";
        return $"{lead} {next}";
    }
}

public sealed class PerformanceDashboardVm
{
    public SimulationReport? Latest { get; init; }
    public IReadOnlyList<SimulationReport> Reports { get; init; } = [];
    public IReadOnlyList<SimulationReport> ChartReports { get; init; } = [];
    public IReadOnlyList<PilotAchievement> Achievements { get; init; } = [];
    public int AverageScore { get; init; }
    public int BestScore { get; init; }
    public int Improvement { get; init; }
}

public static class PerformanceFeed
{
    public static event Action<string>? ReportSaved;
    public static void Notify(string userId) => ReportSaved?.Invoke(userId);
}