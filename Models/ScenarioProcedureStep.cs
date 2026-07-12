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
}