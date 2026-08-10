using AeroResponse.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoPilotAchievement
{
    [BsonId]
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "🏆";
    public DateTime EarnedAt { get; set; }

    public static MongoPilotAchievement FromModel(PilotAchievement achievement) => new()
    {
        Id = achievement.Id,
        UserId = achievement.UserId,
        Code = achievement.Code,
        Name = achievement.Name,
        Description = achievement.Description,
        Icon = achievement.Icon,
        EarnedAt = achievement.EarnedAt
    };

    public PilotAchievement ToModel() => new()
    {
        Id = Id,
        UserId = UserId,
        Code = Code,
        Name = Name,
        Description = Description,
        Icon = Icon,
        EarnedAt = EarnedAt
    };
}
