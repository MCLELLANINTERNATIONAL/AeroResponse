using AeroResponse.Data;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public class SimulationScenarioDataService(
    ApplicationDbContext context)
{
    public async Task<IReadOnlyList<EmergencyScenario>>
        GetActiveScenariosAsync(
            CancellationToken cancellationToken = default)
    {
        return await context.EmergencyScenarios
            .AsNoTracking()
            .Where(scenario => scenario.IsActive)
            .OrderBy(scenario => scenario.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<EmergencyScenario?>
        GetByEmergencyTypeAsync(
            string emergencyType,
            CancellationToken cancellationToken = default)
    {
        return context.EmergencyScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                scenario =>
                    scenario.IsActive &&
                    scenario.EmergencyType == emergencyType,
                cancellationToken);
    }

    public async Task<List<ScenarioProcedureStep>>
        GetProcedureStepsAsync(
            EmergencyScenario scenario,
            string aircraftName,
            CancellationToken cancellationToken = default)
    {
        var databaseSteps =
            await context.ScenarioProcedureSteps
                .AsNoTracking()
                .Where(step =>
                    step.EmergencyScenarioId == scenario.Id &&
                    (
                        step.AircraftType == aircraftName ||
                        step.AircraftType == string.Empty ||
                        step.AircraftType == "All"
                    ))
                .OrderBy(step => step.StepOrder)
                .ToListAsync(cancellationToken);

        if (databaseSteps.Count > 0)
        {
            return databaseSteps;
        }

        return BuildStepsFromExpectedProcedure(
            scenario,
            aircraftName);
    }

    private static List<ScenarioProcedureStep>
        BuildStepsFromExpectedProcedure(
            EmergencyScenario scenario,
            string aircraftName)
    {
        var instructions =
            SplitProcedureText(
                scenario.ExpectedProcedure);

        return instructions
            .Select((instruction, index) =>
                new ScenarioProcedureStep
                {
                    EmergencyScenarioId = scenario.Id,
                    AircraftType = aircraftName,
                    StepOrder = index + 1,
                    Instruction = instruction,
                    CorrectAction = instruction,
                    IsSafetyCritical = false
                })
            .ToList();
    }

    private static IReadOnlyList<string>
        SplitProcedureText(
            string procedure)
    {
        if (string.IsNullOrWhiteSpace(procedure))
        {
            return [];
        }

        var normalized =
            procedure
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

        var lines =
            normalized
                .Split(
                    '\n',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(RemoveListPrefix)
                .Where(line =>
                    !string.IsNullOrWhiteSpace(line))
                .ToList();

        if (lines.Count > 1)
        {
            return lines;
        }

        return normalized
            .Split(
                [
                    ". ",
                    "; ",
                    "•"
                ],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(RemoveListPrefix)
            .Select(EnsureSentenceEnding)
            .Where(line =>
                !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string RemoveListPrefix(
        string value)
    {
        var trimmed = value.Trim();

        var separatorIndex =
            trimmed.IndexOfAny(
                ['.', ')', '-', ':']);

        if (separatorIndex >= 0 &&
            separatorIndex <= 3)
        {
            var possiblePrefix =
                trimmed[..separatorIndex];

            if (possiblePrefix.All(
                    character =>
                        char.IsDigit(character) ||
                        char.IsWhiteSpace(character)))
            {
                trimmed =
                    trimmed[(separatorIndex + 1)..]
                        .Trim();
            }
        }

        return trimmed
            .TrimStart(
                '•',
                '-',
                '*',
                '–',
                '—',
                ' ')
            .Trim();
    }

    private static string EnsureSentenceEnding(
        string value)
    {
        var trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.EndsWith('.') ||
               trimmed.EndsWith('!') ||
               trimmed.EndsWith('?')
            ? trimmed
            : $"{trimmed}.";
    }
}