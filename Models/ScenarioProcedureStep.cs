namespace AeroResponse.Models;

public class ScenarioProcedureStep
{
    public int Id { get; set; }

    public int EmergencyScenarioId { get; set; }

    public string AircraftType { get; set; } = string.Empty;

    public int StepOrder { get; set; }

    public string Instruction { get; set; } = string.Empty;

    public string CorrectAction { get; set; } = string.Empty;

    public bool IsSafetyCritical { get; set; }

    // Latest recommended response time measured from emergency activation.
    public int MaxResponseSeconds { get; set; } = 30;

    // Relative contribution of this step to procedural scoring.
    public int ScoreWeight { get; set; } = 10;

    // Procedure, Decision, Communication, or Checklist.
    public string PerformanceCategory { get; set; } = "Procedure";
}