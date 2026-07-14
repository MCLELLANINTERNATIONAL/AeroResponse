namespace AeroResponse.Simulation.Instruments.Altimeter;

public record AltitudeReading
(
    int Hundreds,
    int Thousands,
    int TenThousands,

    double HundredsAngle,
    double ThousandsAngle,
    double TenThousandsAngle
);