using AeroResponse.Simulation;
using Microsoft.AspNetCore.SignalR;

namespace AeroResponse.Hubs;

public class CockpitHub : Hub
{
    public Task SendCockpitUpdate(CockpitState state)
    {
        return Clients.All.SendAsync("ReceiveCockpitUpdate", state);
    }

    public Task SendEmergencyAlert(string alert)
    {
        return Clients.All.SendAsync("ReceiveEmergencyAlert", alert);
    }
}