using AeroResponse.Models;

namespace AeroResponse.Simulation;

public class ScenarioTriggerEvaluator
{
    public bool ShouldTrigger(
        EmergencyScenario scenario,
        CockpitState state,
        TimeSpan elapsedTime,
        bool manuallyActivated = false)
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

            _ => false
        };
    }
}