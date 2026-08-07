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
    public double BusVoltage { get; set; } = 28;
    public double BatteryVoltage { get; set; } = 24;
    public double ElectricalLoadAmps { get; set; }

    public bool BatteryOnline { get; set; } = true;
    public bool AlternatorOnline { get; set; } = true;
    public bool ElectricalFault { get; set; }

    public double HydraulicPressure { get; set; } = 3000;
    public bool HydraulicPumpOnline { get; set; } = true;
    public bool HydraulicFault { get; set; }

    public bool RadioPowered { get; set; } = true;
    public double RadioFrequency { get; set; } = 121.5;

    public bool SatellitePhonePowered { get; set; }
    public bool SatellitePhoneConnected { get; set; }

    public string? CommunicationStatus { get; set; }

    public bool RadioTransmitting { get; set; }

    public int SatelliteSignalStrength { get; set; } = 4;
    public bool FuelLeakActive { get; set; }

    public int? LeakingFuelTankNumber { get; set; }
    public bool AlternateGearExtensionActivated { get; set; }

    public bool AlternateGearExtensionCompleted { get; set; }

    public List<LandingGearState> LandingGears { get; set; } = [];

    public Dictionary<string, object?> DynamicValues { get; set; } =
    new(StringComparer.OrdinalIgnoreCase);
}