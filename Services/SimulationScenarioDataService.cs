using AeroResponse.Data;
using AeroResponse.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Services;

public class SimulationScenarioDataService(
    ApplicationDbContext context)
{
    public Task<List<EmergencyScenario>>
        GetActiveScenariosAsync(
            CancellationToken cancellationToken = default)
    {
        return context.EmergencyScenarios
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

        // No procedure steps exist in SQLite.
        // Simulation.razor decides whether to use
        // the runtime scenario as a fallback.
        return [];
    }
}