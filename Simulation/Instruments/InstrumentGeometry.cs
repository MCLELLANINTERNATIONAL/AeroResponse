using System.Globalization;

namespace AeroResponse.Simulation.Instruments;

public static class InstrumentGeometry
{
    public static DialPoint GetDialPoint(
        double centerX,
        double centerY,
        double radius,
        double angle)
    {
        var radians = angle * Math.PI / 180.0;

        return new DialPoint(
            centerX + radius * Math.Sin(radians),
            centerY - radius * Math.Cos(radians));
    }

    public static string Format(double value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture);
    }
}