using AeroResponse.Simulation;
using Microsoft.AspNetCore.Components;

namespace AeroResponse.Components.Instruments.Throttle;

public partial class Throttle
{
    [Parameter, EditorRequired]
    public CockpitState State { get; set; } = default!;

    [Parameter, EditorRequired]
    public int EngineCount { get; set; }

    private double GetEnginePower(int engineNumber)
    {
        var engine = State.Engines
            .FirstOrDefault(engine =>
                engine.Number == engineNumber);

        return engine?.Power ?? 0;
    }

    private void HandleThrottleInput(
        int engineNumber,
        ChangeEventArgs args)
    {
        if (!double.TryParse(
                args.Value?.ToString(),
                out var power))
        {
            return;
        }

        var engine = State.Engines
            .FirstOrDefault(engine =>
                engine.Number == engineNumber);

        if (engine is null)
        {
            return;
        }

        engine.Power =
            (int)Math.Clamp(power, 0.0, 100.0);
    }
}