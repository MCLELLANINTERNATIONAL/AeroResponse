using AeroResponse.Simulation;
using Microsoft.AspNetCore.Components;

namespace AeroResponse.Components.Instruments.FireSuppression;

public partial class FireSuppression
{
    [Parameter, EditorRequired]
    public CockpitState State { get; set; } = default!;

    private void ActivateSuppression()
    {
        if (State.FireSuppressionActivated)
        {
            return;
        }

        State.FireSuppressionActivated = true;
    }
}