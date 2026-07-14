namespace AeroResponse.Simulation.Instruments.ArtificialHorizon;

public static class ArtificialHorizonMath
{
    private const double MinimumPitch = -90;
    private const double MaximumPitch = 90;
    private const double MinimumBank = -180;
    private const double MaximumBank = 180;

    private const double PixelsPerPitchDegree = 3.0;

    public static AttitudeReading GetReading(
        double pitch,
        double bank)
    {
        var clampedPitch = Math.Clamp(
            pitch,
            MinimumPitch,
            MaximumPitch);

        var clampedBank = Math.Clamp(
            bank,
            MinimumBank,
            MaximumBank);

        return new AttitudeReading(
            Pitch: clampedPitch,
            Bank: clampedBank,

            // Nose-up attitude moves the visible horizon downward.
            HorizonOffset:
                clampedPitch * PixelsPerPitchDegree,

            // The horizon moves opposite the aircraft's bank.
            HorizonRotation:
                -clampedBank
        );
    }
    public static IReadOnlyList<BankMark> GetBankMarks()
    {
        return
        [
            new(-60, true,  true),
            new(-45, true,  true),
            new(-30, true,  true),
            new(-20, false, false),
            new(-10, false, false),

            new(10, false, false),
            new(20, false, false),
            new(30, true,  true),
            new(45, true,  true),
            new(60, true,  true)
        ];
    }
    public static IReadOnlyList<PitchMark> GetPitchMarks()
    {
        return
        [
            new(-30, true),
            new(-25, false),
            new(-20, true),
            new(-15, false),
            new(-10, true),
            new(-5, false),

            new(5, false),
            new(10, true),
            new(15, false),
            new(20, true),
            new(25, false),
            new(30, true)
        ];
    }
}