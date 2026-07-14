namespace AeroResponse.Simulation.Instruments.TurnCoordinator;

public static class TurnIndicatorMath
{
    private const double MinimumTurnRate = -3.0;
    private const double MaximumTurnRate = 3.0;

    private const double MaximumIndicatorRotation = 30.0;

    private const double MinimumSlip = -1.0;
    private const double MaximumSlip = 1.0;

    private const double MaximumBallOffset = 35.0;

    public static TurnCoordinatorReading GetReading(
        double turnRate,
        double slip)
    {
        var clampedTurnRate = Math.Clamp(
            turnRate,
            MinimumTurnRate,
            MaximumTurnRate);

        var clampedSlip = Math.Clamp(
            slip,
            MinimumSlip,
            MaximumSlip);

        var indicatorRotation =
            clampedTurnRate / MaximumTurnRate *
            MaximumIndicatorRotation;

        var slipBallOffset =
            clampedSlip * MaximumBallOffset;

        return new TurnCoordinatorReading(
            IndicatorRotation: indicatorRotation,
            SlipBallOffset: slipBallOffset);
    }
}