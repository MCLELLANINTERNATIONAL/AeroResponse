namespace AeroResponse.Simulation.Layouts;

public class VerticalSpeedIndicatorLayout
{
    public int MinimumVerticalSpeed { get; set; } = -10000;

    public int MaximumVerticalSpeed { get; set; } = 10000;

    public double LagSeconds { get; set; } = Random.Shared.Next(6, 9);

    public List<VSICalibrationPoint> CalibrationPoints { get; set; } = [];
}