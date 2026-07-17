namespace AeroResponse.Simulation.Instruments.ArtificialHorizon;

public record AttitudeReading(
    double Pitch,
    double Bank,
    double HorizonOffset,
    double HorizonRotation
);