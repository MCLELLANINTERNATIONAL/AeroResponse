using System.Globalization;
using System.Text.RegularExpressions;
using AeroResponse.Models;

namespace AeroResponse.Simulation.Controls;

public sealed partial class VoiceCommandParser
{
    public CockpitCommandRequest? Parse(
        string transcript,
        IReadOnlyList<CockpitControlDefinition> controls)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return null;

        var normalized = Normalize(transcript);
        var number = ExtractNumber(normalized);

        var best = (from control in controls
                    where control.IsVoiceControllable
                    from command in control.Commands
                    let score = Score(normalized, control, command)
                    where score > 0
                    orderby score descending
                    select new { control, command, score })
                   .FirstOrDefault();

        if (best is null || (best.command.RequiresNumericValue && !number.HasValue))
            return null;

        return new CockpitCommandRequest
        {
            RawText = transcript.Trim(),
            ControlId = best.control.ControlId,
            Command = best.command.Command,
            NumericValue = number,
            Unit = best.command.Unit,
            Confidence = Math.Clamp(best.score / 200d, 0, 1)
        };
    }

    private static int Score(string transcript, CockpitControlDefinition control,
        CockpitControlCommandDefinition command)
    {
        var commandScore = command.VoiceAliases.Append(command.Command)
            .Select(alias => PhraseScore(transcript, Normalize(alias)))
            .DefaultIfEmpty(0).Max();

        var controlScore = control.VoiceAliases
            .Append(control.DisplayName).Append(control.ControlId)
            .Select(alias => PhraseScore(transcript, Normalize(alias)))
            .DefaultIfEmpty(0).Max();

        return commandScore == 0 ? 0 : commandScore + controlScore;
    }

    private static int PhraseScore(string transcript, string phrase)
    {
        if (transcript.Equals(phrase, StringComparison.OrdinalIgnoreCase)) return 120;
        if (transcript.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            return 80 + phrase.Length;

        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matches = words.Count(word => transcript.Contains(word,
            StringComparison.OrdinalIgnoreCase));
        return words.Length > 0 && matches == words.Length ? 50 + matches : 0;
    }

    private static double? ExtractNumber(string transcript)
    {
        var match = NumberRegex().Match(transcript);
        return match.Success && double.TryParse(match.Value, NumberStyles.Float,
            CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static string Normalize(string value) => Regex.Replace(
            value.ToLowerInvariant(), @"[^a-z0-9\.\-\s]", " ")
        .Replace("one", "1").Replace("two", "2").Replace("three", "3")
        .Replace("four", "4").Replace("five", "5").Replace("zero", "0")
        .Trim();

    [GeneratedRegex(@"-?\d+(?:\.\d+)?")]
    private static partial Regex NumberRegex();
}