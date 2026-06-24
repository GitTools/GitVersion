namespace GitVersion.Formatting;

internal class LabelTokenizer(string input)
{
    private int index;

    public IEnumerable<LabelToken> ParseTokens()
    {
        var tokens = new List<LabelToken>();
        var separatedParsed = false;

        SkipWhitespace();

        while (this.index < input.Length)
        {
            var identifier = ParseIdentifier();

            SkipWhitespace();

            if (string.IsNullOrEmpty(identifier))
            {
                throw new FormatException("Invalid format sequence, expected identifier");
            }

            tokens.Add(ParseToken(identifier));

            separatedParsed = ParseSeparator();

            SkipWhitespace();
        }

        if (separatedParsed)
        {
            throw new FormatException("Invalid format sequence, expected identifier after '??' separator");
        }

        return tokens;
    }

    private static LabelToken ParseToken(string identifier)
    {
        if (identifier.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var name = identifier[4..];
            var (environmentName, environmentFormat) = ParseKeyAndFormat(name);

            return new LabelToken(environmentName, LabelTokenType.Environment, environmentFormat);
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

        while (this.index < input.Length)
        {
            var c = input[this.index];

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
                this.index += 2;
            }
            else
            {
                value.Append(c);
                this.index++;
            }
        }

        if (inQuotes)
        {
            throw new FormatException("Literal value is missing closing quote");
        }

        return value.ToString();
    }

    private bool ParseSeparator()
    {
        var seen = 0;

        while (this.index < input.Length && seen < 2)
        {
            if (input[this.index] != '?')
            {
                throw new FormatException("Expected '??' separator");
            }

            seen++;
            this.index++;
        }

        return seen == 2;
    }

    private void SkipWhitespace()
    {
        while (this.index < input.Length)
        {
            if (!char.IsWhiteSpace(input[this.index]))
            {
                break;
            }

            this.index++;
        }
    }

    private bool ParseQuote()
    {
        if (IsQuote())
        {
            this.index++;

            return true;
        }

        return false;
    }

    private bool IsQuote() => this.index < input.Length && input[this.index] == '"';

    private bool IsEscapeQuote() => this.index + 1 < input.Length && input[this.index] == '\\' && input[this.index + 1] == '"';
}
