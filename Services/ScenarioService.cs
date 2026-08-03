using AeroResponse.Data;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public sealed class ScenarioService(
    ApplicationDbContext context)
{
    public async Task<IReadOnlyList<EmergencyScenario>>
        GetAllAsync()
    {
        return await context.EmergencyScenarios
            .AsNoTracking()
            .OrderBy(scenario => scenario.Title)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EmergencyScenario>>
        GetActiveAsync()
    {
        return await context.EmergencyScenarios
            .AsNoTracking()
            .Where(scenario => scenario.IsActive)
            .OrderBy(scenario => scenario.Title)
            .ToListAsync();
    }

    public Task<EmergencyScenario?>
        GetByEmergencyTypeAsync(
            string emergencyType)
    {
        return context.EmergencyScenarios
            .AsNoTracking()
            .Include(scenario => scenario.ProcedureSteps)
            .FirstOrDefaultAsync(scenario =>
                scenario.IsActive &&
                scenario.EmergencyType == emergencyType);
    }

    public Task<EmergencyScenario?>
        GetByIdAsync(
            int id)
    {
        return context.EmergencyScenarios
            .AsNoTracking()
            .Include(scenario => scenario.ProcedureSteps)
            .FirstOrDefaultAsync(scenario =>
                scenario.Id == id);
    }

    public async Task<EmergencyScenario>
        CreateAsync(
            EmergencyScenario scenario,
            IReadOnlyList<ScenarioProcedureStep> steps)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        scenario.CreatedAt = DateTime.UtcNow;

        scenario.ProcedureSteps = NormalizeSteps(
            steps,
            scenario.TimeLimitSeconds);

        context.EmergencyScenarios.Add(scenario);

        await context.SaveChangesAsync();

        return scenario;
    }

    public async Task UpdateAsync(
        EmergencyScenario scenario,
        IReadOnlyList<ScenarioProcedureStep> steps)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var existing = await context.EmergencyScenarios
            .Include(item => item.ProcedureSteps)
            .FirstOrDefaultAsync(item =>
                item.Id == scenario.Id)
            ?? throw new KeyNotFoundException(
                $"Scenario {scenario.Id} was not found.");

        existing.Title = scenario.Title;
        existing.EmergencyType = scenario.EmergencyType;
        existing.Description = scenario.Description;
        existing.Difficulty = scenario.Difficulty;
        existing.TriggerCondition = scenario.TriggerCondition;
        existing.TriggerType = scenario.TriggerType;
        existing.TriggerDelaySeconds = scenario.TriggerDelaySeconds;
        existing.TriggerAltitudeFeet = scenario.TriggerAltitudeFeet;
        existing.TriggerAirspeedKnots = scenario.TriggerAirspeedKnots;
        existing.TriggerFlightPhase = scenario.TriggerFlightPhase;
        existing.RequiresManualActivation =
            scenario.RequiresManualActivation;
        existing.TimeLimitSeconds = scenario.TimeLimitSeconds;
        existing.SuccessCondition = scenario.SuccessCondition;
        existing.FailureCondition = scenario.FailureCondition;
        existing.ScoringRules = scenario.ScoringRules;
        existing.ExpectedProcedure = scenario.ExpectedProcedure;
        existing.IsActive = scenario.IsActive;

        context.ScenarioProcedureSteps.RemoveRange(
            existing.ProcedureSteps);

        existing.ProcedureSteps = NormalizeSteps(
            steps,
            scenario.TimeLimitSeconds);

        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var scenario = await context.EmergencyScenarios
            .FirstOrDefaultAsync(item => item.Id == id);

        if (scenario is null)
        {
            return false;
        }

        context.EmergencyScenarios.Remove(scenario);

        await context.SaveChangesAsync();

        return true;
    }

    public Task<bool> ExistsAsync(int id)
    {
        return context.EmergencyScenarios
            .AnyAsync(item => item.Id == id);
    }

    private static List<ScenarioProcedureStep>
        NormalizeSteps(
            IReadOnlyList<ScenarioProcedureStep> steps,
            int scenarioTimeLimit)
    {
        return steps
            .Where(step =>
                !string.IsNullOrWhiteSpace(step.Instruction) &&
                !string.IsNullOrWhiteSpace(step.CorrectAction))
            .Select((step, index) =>
                new ScenarioProcedureStep
                {
                    AircraftType =
                        string.IsNullOrWhiteSpace(step.AircraftType)
                            ? "All"
                            : step.AircraftType.Trim(),

                    StepOrder = index + 1,

                    Instruction =
                        step.Instruction.Trim(),

                    CorrectAction =
                        step.CorrectAction.Trim(),

                    IsSafetyCritical =
                        step.IsSafetyCritical,

                    MaxResponseSeconds = Math.Clamp(
                        step.MaxResponseSeconds,
                        1,
                        scenarioTimeLimit),

                    ScoreWeight = Math.Clamp(
                        step.ScoreWeight,
                        1,
                        100),

                    PerformanceCategory =
                        string.IsNullOrWhiteSpace(
                            step.PerformanceCategory)
                            ? "Procedure"
                            : step.PerformanceCategory
                })
            .ToList();
    }
}