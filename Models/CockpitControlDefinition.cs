namespace AeroResponse.Models;

public sealed class CockpitControlDefinition
{
    public string ControlId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public int? EngineNumber { get; set; }
    public bool IsVoiceControllable { get; set; } = true;
    public bool IsSafetyCritical { get; set; }
    public List<string> VoiceAliases { get; set; } = [];
    public List<CockpitControlCommandDefinition> Commands { get; set; } = [];
}