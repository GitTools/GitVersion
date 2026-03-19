using GitVersion.VersionCalculation;
using SharpYaml;
using SharpYaml.Serialization;

namespace GitVersion.Configuration;

internal sealed class VersionStrategiesConverter : YamlConverter<VersionStrategies[]>
{
    public static YamlConverter Instance { get; } = new VersionStrategiesConverter();

    public override VersionStrategies[] Read(YamlReader reader)
    {
        List<VersionStrategies> strategies = [];

        if (reader.TokenType == YamlTokenType.StartSequence)
        {
            reader.Read();
            while (reader.TokenType != YamlTokenType.EndSequence)
            {
                if (reader.TokenType != YamlTokenType.Scalar)
                {
                    throw new YamlException(reader.SourceName, reader.Start, reader.End, "Expected a scalar value while reading version strategies.");
                }

                strategies.Add(ParseStrategy(reader.GetScalarValue()));
                reader.Read();
            }

            reader.Read();
            return [.. strategies];
        }

        if (reader.TokenType != YamlTokenType.Scalar)
        {
            throw new YamlException(reader.SourceName, reader.Start, reader.End, "Expected a scalar or sequence while reading version strategies.");
        }

        var scalar = reader.GetScalarValue();
        reader.Read();

        foreach (var item in SplitScalarList(scalar))
        {
            strategies.Add(ParseStrategy(item));
        }

        return [.. strategies];
    }

    public override void Write(YamlWriter writer, VersionStrategies[]? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartSequence();
        foreach (var strategy in value)
        {
            writer.WriteString(strategy.ToString());
        }
        writer.WriteEndSequence();
    }

    private static IEnumerable<string> SplitScalarList(string scalar)
    {
        var trimmed = scalar.Trim();
        if (trimmed.Length == 0) yield break;

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            trimmed = trimmed[1..^1];
        }

        foreach (var item in trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            yield return item;
        }
    }

    private static VersionStrategies ParseStrategy(string value)
    {
        if (Enum.TryParse<VersionStrategies>(value, ignoreCase: false, out var exactMatch))
        {
            return exactMatch;
        }

        var normalizedValue = Normalize(value);
        foreach (var enumValue in Enum.GetValues<VersionStrategies>().Where(enumValue => Normalize(enumValue.ToString()) == normalizedValue))
        {
            return enumValue;
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown version strategy value.");
    }

    private static string Normalize(string value)
        => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
