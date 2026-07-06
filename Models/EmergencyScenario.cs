namespace AeroResponse.Models;

public class EmergencyScenario
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EmergencyType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Beginner";

    public string TriggerCondition { get; set; } = string.Empty;

    public string ExpectedProcedure { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}