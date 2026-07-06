namespace AeroResponse.Models;

public class Aircraft
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Manufacturer { get; set; } = string.Empty;

    public string AircraftType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int MaxAltitude { get; set; }

    public int CruiseSpeed { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}