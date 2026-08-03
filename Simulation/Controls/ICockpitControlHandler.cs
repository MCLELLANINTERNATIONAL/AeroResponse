using AeroResponse.Models;

namespace AeroResponse.Simulation.Controls;

public interface ICockpitControlHandler
{
    bool CanHandle(CockpitControlDefinition definition);

    CockpitCommandResult Execute(
        CockpitControlDefinition definition,
        CockpitCommandRequest request,
        CockpitState state);
}