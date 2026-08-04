using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Controls;

public sealed class CockpitCommandService(
    CockpitControlCatalog catalog,
    VoiceCommandParser parser,
    IEnumerable<ICockpitControlHandler> handlers)
{
    public CockpitCommandRequest? Parse(string transcript, CockpitLayoutDefinition layout) =>
        parser.Parse(transcript, catalog.Build(layout));

    public CockpitCommandResult Execute(CockpitCommandRequest request,
        CockpitLayoutDefinition layout, CockpitState state)
    {
        var control = catalog.Build(layout).FirstOrDefault(item =>
            item.ControlId.Equals(request.ControlId, StringComparison.OrdinalIgnoreCase));

        if (control is null)
            return CockpitCommandResult.Failure("Cockpit control is unavailable.");

        var handler = handlers.FirstOrDefault(item => item.CanHandle(control));
        return handler is null
            ? CockpitCommandResult.Failure($"No handler exists for {control.DisplayName}.")
            : handler.Execute(control, request, state);
    }
}