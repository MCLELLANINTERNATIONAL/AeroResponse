namespace AeroResponse.Simulation.Instruments.VerticalSpeedIndicator;

public record VSIReading(
    int ActualVerticalSpeed,
    double DisplayedVerticalSpeed,
    double NeedleRotation
);