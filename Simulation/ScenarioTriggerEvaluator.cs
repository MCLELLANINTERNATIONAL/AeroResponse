using AeroResponse.Models;

namespace AeroResponse.Simulation;

public class ScenarioTriggerEvaluator
{
    public bool ShouldTrigger(
        EmergencyScenario scenario,
        CockpitState state,
        TimeSpan elapsedTime,
        bool manuallyActivated = false,
        string? pilotAction = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(state);

        return scenario.TriggerType switch
        {
            "Immediate" => true,

            "Time" =>
                scenario.TriggerDelaySeconds.HasValue &&
                elapsedTime.TotalSeconds >=
                    scenario.TriggerDelaySeconds.Value,

            "Altitude" =>
                scenario.TriggerAltitudeFeet.HasValue &&
                state.Altitude >=
                    scenario.TriggerAltitudeFeet.Value,

            "Airspeed" =>
                scenario.TriggerAirspeedKnots.HasValue &&
                state.Airspeed >=
                    scenario.TriggerAirspeedKnots.Value,

            "Flight Phase" =>
                !string.IsNullOrWhiteSpace(
                    scenario.TriggerFlightPhase) &&
                string.Equals(
                    state.FlightPhase,
                    scenario.TriggerFlightPhase,
                    StringComparison.OrdinalIgnoreCase),

            "Manual" =>
                manuallyActivated,

            "Action" =>
                !string.IsNullOrWhiteSpace(
                    scenario.TriggerAction) &&
                !string.IsNullOrWhiteSpace(
                    pilotAction) &&
                string.Equals(
                    scenario.TriggerAction,
                    pilotAction,
                    StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}