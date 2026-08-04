namespace AeroResponse.Models;

public sealed class CockpitControlCommandDefinition
{
    public string Command { get; set; } = string.Empty;
    public List<string> VoiceAliases { get; set; } = [];
    public double? MinimumValue { get; set; }
    public double? MaximumValue { get; set; }
    public string? Unit { get; set; }
    public bool RequiresNumericValue { get; set; }
}