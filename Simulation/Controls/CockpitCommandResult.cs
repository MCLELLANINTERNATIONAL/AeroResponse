namespace AeroResponse.Simulation.Controls;

public sealed class CockpitCommandResult
{
    public bool Succeeded { get; init; }
    public string ActionName { get; init; } = string.Empty;
    public string SpokenFeedback { get; init; } = string.Empty;
    public string? ControlId { get; init; }

    public static CockpitCommandResult Success(string actionName, string feedback,
        string controlId) => new()
    {
        Succeeded = true,
        ActionName = actionName,
        SpokenFeedback = feedback,
        ControlId = controlId
    };

    public static CockpitCommandResult Failure(string feedback) => new()
    {
        Succeeded = false,
        SpokenFeedback = feedback
    };
}