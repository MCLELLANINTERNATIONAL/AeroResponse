namespace AeroResponse.Models;

public class FlightLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int AircraftId { get; set; }

    public int EmergencyScenarioId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = "In Progress";

    public string Notes { get; set; } = string.Empty;
}