using AeroResponse.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoPilotAchievement
{
    // Support legacy integer _id values while all new achievements use
    // Mongo-native ObjectIds. This prevents a reset SQL identity value from
    // replacing an unrelated MongoDB achievement.
    [BsonId]
    public BsonValue Id { get; set; } =
        ObjectId.GenerateNewId();

    [BsonIgnoreIfNull]
    public int? LegacySqlAchievementId { get; set; }

    public string UserId { get; set; } =
        string.Empty;

    public string Code { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string Icon { get; set; } =
        "🏆";

    public DateTime EarnedAt { get; set; }

    public static MongoPilotAchievement FromModel(
        PilotAchievement achievement) =>
        new()
        {
            Id =
                ObjectId.GenerateNewId(),

            LegacySqlAchievementId =
                achievement.Id > 0
                    ? achievement.Id
                    : null,

            UserId =
                achievement.UserId,

            Code =
                achievement.Code,

            Name =
                achievement.Name,

            Description =
                achievement.Description,

            Icon =
                achievement.Icon,

            EarnedAt =
                achievement.EarnedAt
        };

    public PilotAchievement ToModel() =>
        new()
        {
            Id =
                LegacySqlAchievementId
                ?? GetLegacyIntegerId(),

            UserId =
                UserId,

            Code =
                Code,

            Name =
                Name,

            Description =
                Description,

            Icon =
                Icon,

            EarnedAt =
                EarnedAt
        };

    private int GetLegacyIntegerId()
    {
        if (Id.IsInt32)
        {
            return Id.AsInt32;
        }

        if (Id.IsInt64 &&
            Id.AsInt64 >= int.MinValue &&
            Id.AsInt64 <= int.MaxValue)
        {
            return (int)Id.AsInt64;
        }

        return 0;
    }
}