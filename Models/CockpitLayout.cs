using System.ComponentModel.DataAnnotations;

namespace AeroResponse.Models;

public class CockpitLayout
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Rows { get; set; } = 2;

    [Range(1, 10)]
    public int Columns { get; set; } = 3;

    public List<InstrumentDefinition> Instruments { get; set; } = [];

    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}