namespace AeroResponse.Services;

public sealed class AiInstructorFeedback
{
    public string Severity { get; init; } = "Information";
    public string Message { get; init; } = string.Empty;
    public string? RecommendedAction { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}