namespace AeroResponse.Simulation;

public class EmergencyEngine
{
    public string GetEmergencyStatus(CockpitState state)
    {
        return state.AlertMessage;
    }
}