using System.Reflection.Metadata.Ecma335;

namespace GitVersion.Formatting;

internal class LabelTokenizer(string input)
{
    private int index;

    public IEnumerable<LabelToken> ParseTokens()
    {
        var tokens = new List<LabelToken>();

        SkipWhitespace();

        while (index < input.Length)
        {
            var identifier = ParseIdentifier();

            SkipWhitespace();

            if (string.IsNullOrEmpty(identifier))
            {
                throw new FormatException("Invalid format sequence, expected identifier");
            }

            tokens.Add(ParseToken(identifier));

            ParseSeparator();
            SkipWhitespace();
        }

        return tokens;
    }

    private static LabelToken ParseToken(string identifier)
    {
        if (identifier.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var name = identifier[4..];
            var (environmentName, environemntFormat) = ParseKeyAndFormat(name);

            return new LabelToken(environmentName, LabelTokenType.Environment, environemntFormat);
        }

        if (identifier.StartsWith('"') && identifier.EndsWith('"'))
        {
            return new LabelToken(identifier[1..^1], LabelTokenType.Literal);
        }

        var (propertyName, propertyFormat) = ParseKeyAndFormat(identifier);

        return new LabelToken(propertyName, LabelTokenType.Property, propertyFormat);
    }

    private static (string Key, string? Format) ParseKeyAndFormat(string identifier)
    {
        var parts = identifier.Split(':');

        if (parts.Length > 2)
        {
            throw new FormatException($"Invalid format string: {identifier}");
        }

        if (parts is [var key, var format])
        {
            return (key, format);
        }

        return (identifier, null);
    }

    private string ParseIdentifier()
    {
        var value = new StringBuilder();
        var inQuotes = ParseQuote();

        if (inQuotes)
        {
            value.Append('"');
        }

        while (index < input.Length)
        {
            var c = input[index];

            if (!inQuotes && IsQuote())
            {
                throw new FormatException("Literal value was not correctly quoted");
            }

            if (ParseQuote())
            {
                value.Append('"');

                return value.ToString();
            }

            if (!inQuotes && (char.IsWhiteSpace(c) || c == '?'))
            {
                return value.ToString();
            }

            if (IsEscapeQuote())
            {
                value.Append('"');
                index += 2;
            }
            else
            {
                value.Append(c);
                index++;
            }
        }

        if (inQuotes)
        {
            throw new FormatException("Literal value is missing closing quote");
        }

        return value.ToString();
    }

    private void ParseSeparator()
    {
        var seen = 0;

        while (this.index < input.Length && seen < 2)
        {
            if (input[index] != '?')
            {
                throw new FormatException("Expected '??' separator");
            }

            seen++;
            index++;
        }
    }

    private void SkipWhitespace()
    {
        while (index < input.Length)
        {
            if (!char.IsWhiteSpace(input[index]))
            {
                break;
            }

            index++;
        }
    }

    private bool ParseQuote()
    {
        if (IsQuote())
        {
            index++;

            return true;
        }

        return false;
    }

    private bool IsQuote() => index < input.Length && input[index] == '"';

    private bool IsEscapeQuote() => this.index + 1 < input.Length && input[index] == '\\' && input[this.index + 1] == '"';
}
