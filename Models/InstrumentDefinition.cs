namespace AeroResponse.Models;

public class InstrumentDefinition
{
    public InstrumentType Type { get; set; }

    public int GridRow { get; set; }

    public int GridColumn { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColumnSpan { get; set; } = 1;

    // Dynamic Level-4 control metadata.
    public string ControlId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsVoiceControllable { get; set; } = true;

    public List<string> VoiceAliases { get; set; } = [];

    public List<CockpitControlCommandDefinition> VoiceCommands { get; set; } = [];
}