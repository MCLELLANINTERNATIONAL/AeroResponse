namespace AeroResponse.Simulation.Layouts;

public class AircraftStateDefaults
{
    public double CruiseAirspeed { get; set; }

    public double CruiseAltitude { get; set; }

    public double DefaultHeading { get; set; }

    public double DefaultVerticalSpeed { get; set; }

    public double DefaultPitch { get; set; }

    public double DefaultBank { get; set; }

    public int NormalEnginePower { get; set; } = 75;

    public int FuelPercentage { get; set; } = 75;
}