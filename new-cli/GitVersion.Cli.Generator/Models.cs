namespace GitVersion;

internal record CommandInfo
{
    public string? ParentCommand { get; init; }
    public required string CommandTypeNamespace { get; init; }
    public required string CommandTypeName { get; init; }
    public required string CommandName { get; init; }
    public required string CommandDescription { get; init; }
    public required string SettingsTypeName { get; init; }
    public required string SettingsTypeNamespace { get; init; }
    public required SettingsPropertyInfo[] SettingsProperties { get; init; } = [];
}

internal record SettingsPropertyInfo
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string OptionName { get; init; }
    public required string[] Aliases { get; init; }
    public required string Description { get; init; }
    public required bool Required { get; init; }
}
