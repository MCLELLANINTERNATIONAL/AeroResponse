namespace AeroResponse.Simulation.Instruments.Altimeter;

public static class AltimeterMath
{
    public static AltitudeReading Calculate(int altitude)
    {
        altitude = Math.Max(0, altitude);

        var hundreds =
            (altitude / 100) % 10;

        var thousands =
            (altitude / 1000) % 10;

        var tenThousands =
            altitude / 10000;

        var hundredsAngle =
            (altitude % 1000) / 1000.0 * 360.0;

        var thousandsAngle =
            (altitude % 10000) / 10000.0 * 360.0;

        var tenThousandsAngle =
            (altitude / 100000.0) * 360.0;

        return new AltitudeReading(
            hundreds,
            thousands,
            tenThousands,
            hundredsAngle,
            thousandsAngle,
            tenThousandsAngle);
    }
}