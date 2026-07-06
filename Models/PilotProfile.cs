namespace AeroResponse.Models;

public class PilotProfile
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string ExperienceLevel { get; set; } = "Trainee";

    public int TotalSimulationsCompleted { get; set; }

    public double AverageScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}