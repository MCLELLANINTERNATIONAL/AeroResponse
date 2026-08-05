using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Instruments.AirspeedIndicator;

public static class AirspeedIndicatorMath
{
    public static AirspeedReading GetReading(
        int airspeed,
        CockpitLayoutDefinition layout)
    {
        var config = layout.Airspeed;

        if (config.MaximumSpeed <= config.MinimumSpeed)
        {
            return new AirspeedReading(
                0,
                -120);
        }

        airspeed = Math.Clamp(
            airspeed,
            config.MinimumSpeed,
            config.MaximumSpeed);

        var percentage =
            (double)(airspeed - config.MinimumSpeed) /
            (config.MaximumSpeed - config.MinimumSpeed);

        var angle =
            config.MinAirspeedAngle +
            percentage *
            (config.MaxAirspeedAngle -
             config.MinAirspeedAngle);

        return new AirspeedReading(
            airspeed,
            angle);
    }
}