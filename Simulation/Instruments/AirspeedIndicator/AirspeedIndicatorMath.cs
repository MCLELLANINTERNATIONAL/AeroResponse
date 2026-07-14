using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Instruments.AirspeedIndicator;

public static class AirspeedIndicatorMath
{

    public static AirspeedReading GetReading(
        int airspeed,
        CockpitLayoutDefinition layout)
    {
        var config = layout.Airspeed;
        airspeed = Math.Clamp(airspeed,config.MinimumSpeed, config.MaximumSpeed);

        double percentage =
            (double)(airspeed - config.MinimumSpeed) /
            (config.MaximumSpeed - config.MinimumSpeed);

        double angle =
            config.MinAirspeedAngle +
            percentage * (config.MaxAirspeedAngle - config.MinAirspeedAngle);

        return new AirspeedReading(
            airspeed,
            angle);
    }
}