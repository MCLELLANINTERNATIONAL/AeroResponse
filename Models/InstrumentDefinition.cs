namespace AeroResponse.Models;

public class InstrumentDefinition
{
    public InstrumentType Type { get; set; }

    public int GridRow { get; set; }

    public int GridColumn { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColumnSpan { get; set; } = 1;
}