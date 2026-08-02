using AeroResponse.Simulation;
using Microsoft.AspNetCore.Components;

namespace AeroResponse.Components.Instruments.Rudder;

public partial class Rudder
{
    [Parameter, EditorRequired]
    public CockpitState State { get; set; } = default!;

    private double RudderPosition =>
        State.RudderPosition;

    private void HandleRudderInput(
        ChangeEventArgs args)
    {
        if (!double.TryParse(
                args.Value?.ToString(),
                out var value))
        {
            return;
        }

        State.RudderPosition =
            Math.Clamp(value, -1.0, 1.0);
    }

    private void CenterRudder()
    {
        State.RudderPosition = 0;
    }

    private string GetRudderDisplay()
    {
        if (Math.Abs(State.RudderPosition) < 0.01)
        {
            return "Centered";
        }

        var percentage =
            Math.Abs(State.RudderPosition) * 100;

        var direction =
            State.RudderPosition < 0
                ? "Left"
                : "Right";

        return $"{direction} {percentage:0}%";
    }
}