namespace AeroResponse.Simulation.Layouts;
public class ArtificialHorizonLayout
{
    public double MinimumPitch { get; set; } = -90;

    public double MaximumPitch { get; set; } = 90;

    public double MinimumBank { get; set; } = -180;

    public double MaximumBank { get; set; } = 180;

    public double PixelsPerPitchDegree { get; set; } = 3;
}