using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public static class ScenarioStartConditionEvaluator
{
    public static bool IsSatisfied(
        ScenarioStartCondition condition,
        CockpitState state,
        Aircraft? aircraft = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(state);

        if (condition.MinimumAltitude.HasValue &&
            state.Altitude <
            condition.MinimumAltitude.Value)
        {
            return false;
        }

        if (condition.MaximumAltitude.HasValue &&
            state.Altitude >
            condition.MaximumAltitude.Value)
        {
            return false;
        }

        if (condition.MinimumAirspeed.HasValue &&
            state.Airspeed <
            condition.MinimumAirspeed.Value)
        {
            return false;
        }

        if (condition.MaximumAirspeed.HasValue &&
            state.Airspeed >
            condition.MaximumAirspeed.Value)
        {
            return false;
        }

        if (condition.MinimumVerticalSpeed.HasValue &&
            state.VerticalSpeed <
            condition.MinimumVerticalSpeed.Value)
        {
            return false;
        }

        if (condition.MaximumVerticalSpeed.HasValue &&
            state.VerticalSpeed >
            condition.MaximumVerticalSpeed.Value)
        {
            return false;
        }

        if (condition.MinimumAverageEnginePower.HasValue)
        {
            if (state.Engines.Count == 0)
            {
                return false;
            }

            var averagePower =
                state.Engines.Average(
                    engine => engine.Power);

            if (averagePower <
                condition.MinimumAverageEnginePower.Value)
            {
                return false;
            }
        }

        if (condition.MinimumFuelPercentage.HasValue &&
            state.FuelPercentage <
            condition.MinimumFuelPercentage.Value)
        {
            return false;
        }

        if (condition.MinimumHydraulicPressure.HasValue &&
            state.HydraulicPressure <
            condition.MinimumHydraulicPressure.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(
                condition.RequiredFlightPhase) &&
            !string.Equals(
                state.FlightPhase,
                condition.RequiredFlightPhase,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (condition.AllowedFlightPhases.Count > 0 &&
            !condition.AllowedFlightPhases.Any(
                phase =>
                    string.Equals(
                        phase,
                        state.FlightPhase,
                        StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (condition.RequiresEnginesRunning == true &&
            !state.Engines.Any(
                engine => engine.Running))
        {
            return false;
        }

        if (condition.RequiresAircraftAirborne == true &&
            state.Altitude <= 0)
        {
            return false;
        }

        if (condition.RequiresElectricalSystemOnline == true &&
            state.ElectricalFault)
        {
            return false;
        }

        if (condition.RequiresRetractableLandingGear == true)
        {
            if (aircraft is null)
            {
                return false;
            }

            if (!HasRetractableLandingGear(
                    aircraft.LandingGearConfig.Kind))
            {
                return false;
            }
        }

        return true;
    }
    private static bool HasRetractableLandingGear(
        LandingGearKind kind)
    {
        return kind switch
        {
            LandingGearKind.RetractableTricycle => true,
            LandingGearKind.MultiBogey => true,
            LandingGearKind.Tandem => true,

            LandingGearKind.FixedTricycle => false,
            LandingGearKind.Tailwheel => false,
            LandingGearKind.Floats => false,
            LandingGearKind.Skis => false,

            _ => false
        };
    }
}