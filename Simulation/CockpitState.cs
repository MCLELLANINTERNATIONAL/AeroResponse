using Microsoft.EntityFrameworkCore.Storage;

namespace AeroResponse.Simulation;

public class CockpitState
{
    public double Airspeed { get; set; } = 200;

    public double Altitude { get; set; } = 12000;

    public double Heading { get; set; } = 270;

    public double VerticalSpeed { get; set; } = 0;

    public double DisplayedVerticalSpeed { get; set; } = 0;

    public double Pitch { get; set; } = 0;

    public double Bank { get; set; } = 0;

    public double Slip { get; set; } = 0;

    public double TurnRate { get; set; } = 0;

    public string FlightPhase { get; set; } = "Cruise";

    public List<EngineState> Engines { get; set; } = [];
    public List<BrakePressureState> Brakes { get; set; } = [];
    public double BrakePressure
    {
        get
        {
            var totalPressure = 0.0;
            foreach (var brake in Brakes)
            {
                totalPressure += brake.Pressure;
            }
            return totalPressure;
        }
    }
    public List<FuelState> FuelTanks { get; set; } = [];

    public string AlertMessage { get; set; } = "Systems Normal";

    public double FuelPercentage { get; set; } = 100;
    public double OilPressure { get; set; } = 45;
    public double OilTemperature { get; set; } = 180;
    public double RudderPosition { get; set; } = 0;
    public bool FireSuppressionActivated { get; set; } = false;
    public bool FireDetected { get; set; } = false;
}