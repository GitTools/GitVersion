namespace GitVersion.Formatting;

internal record LabelToken(string Name, LabelTokenType Type, string? Format = null);
