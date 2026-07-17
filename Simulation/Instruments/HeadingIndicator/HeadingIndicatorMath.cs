namespace AeroResponse.Simulation.Instruments.HeadingIndicator;

public static class HeadingIndicatorMath
{
    public static HeadingReading GetReading(double heading)
    {
        var normalizedHeading = NormalizeHeading(heading);

        return new HeadingReading(
            Heading: normalizedHeading,

            // The aircraft reference remains fixed while the compass card
            // rotates opposite the aircraft's heading.
            CompassCardRotation: -normalizedHeading);
    }

    private static double NormalizeHeading(double heading)
    {
        var normalized = heading % 360.0;

        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }
}