using AeroResponse.Simulation.Instruments;

namespace AeroResponse.Simulation.Instruments.HydraulicSystem;

public static class HydraulicSystemMath
{
    public const double MinimumPressure = 0;
    public const double MaximumPressure = 3000;

    public const double MinimumAngle = -120;
    public const double MaximumAngle = 120;

    public static double GetNeedleAngle(double pressure)
    {
        var clamped = Math.Clamp(
            pressure,
            MinimumPressure,
            MaximumPressure);

        var percentage =
            (clamped - MinimumPressure) /
            (MaximumPressure - MinimumPressure);

        return MinimumAngle +
            percentage *
            (MaximumAngle - MinimumAngle);
    }

    public static DialPoint GetDialPoint(
        double radius,
        double angle)
    {
        return InstrumentGeometry.GetDialPoint(
            150,
            150,
            radius,
            angle);
    }
}