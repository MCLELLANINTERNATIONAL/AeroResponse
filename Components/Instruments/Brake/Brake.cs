using AeroResponse.Simulation;
using Microsoft.AspNetCore.Components;

namespace AeroResponse.Components.Instruments.Brake;

public partial class Brake
{
    [Parameter, EditorRequired]
    public CockpitState State { get; set; } = default!;

    private double BrakePressure =>
        State.BrakePressure;

    private void HandleBrakeInput(
        ChangeEventArgs args)
    {
        if (!double.TryParse(
                args.Value?.ToString(),
                out var value))
        {
            return;
        }

        State.BrakePressure =
            Math.Clamp(value, 0, 100);
    }

    private void ApplyFullBrake()
    {
        State.BrakePressure = 100;
    }

    private void ReleaseBrake()
    {
        State.BrakePressure = 0;
    }
}