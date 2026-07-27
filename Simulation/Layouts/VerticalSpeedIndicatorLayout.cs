namespace AeroResponse.Simulation.Layouts;

public class VerticalSpeedIndicatorLayout
{
    public int MinimumVerticalSpeed { get; set; } = -2000;
    public int MaximumVerticalSpeed { get; set; } = 2000;
    public int LagSeconds { get; set; } = 6;
    public List<VSICalibrationPoint> CalibrationPoints { get; set; } = new()
    {
        new(-2000, -235),
        new(-1500, -200),
        new(-1000, -160),
        new(-500, -125),
        new(0, -90),
        new(500, -55),
        new(1000, -20),
        new(1500, 20),
        new(2000, 55)
    };
}