using System.ComponentModel.DataAnnotations;

namespace AeroResponse.Models;

public class PilotAchievement
{
    public int Id { get; set; }

    [MaxLength(450)] public string UserId { get; set; } = string.Empty;

    [MaxLength(80)] public string Code { get; set; } = string.Empty;

    [MaxLength(120)] public string Name { get; set; } = string.Empty;

    [MaxLength(300)] public string Description { get; set; } = string.Empty;

    [MaxLength(40)] public string Icon { get; set; } = "🏆";

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

}