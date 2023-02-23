namespace GitVersion;

internal class CommandInfo : IEquatable<CommandInfo>
{
    public string? ParentCommand { get; init; }
    public required string CommandTypeNamespace { get; init; }
    public required string CommandTypeName { get; init; }
    public required string CommandName { get; init; }
    public required string CommandDescription { get; init; }
    public required string SettingsTypeName { get; init; }
    public required string SettingsTypeNamespace { get; init; }
    public required SettingsPropertyInfo[] SettingsProperties { get; init; } = Array.Empty<SettingsPropertyInfo>();
    public bool Equals(CommandInfo? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return ParentCommand == other.ParentCommand
               && CommandTypeNamespace == other.CommandTypeNamespace
               && CommandTypeName == other.CommandTypeName
               && CommandName == other.CommandName
               && CommandDescription == other.CommandDescription
               && SettingsTypeName == other.SettingsTypeName
               && SettingsTypeNamespace == other.SettingsTypeNamespace
               && SettingsProperties.Equals(other.SettingsProperties);
    }
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((CommandInfo)obj);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (ParentCommand != null ? ParentCommand.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CommandTypeNamespace.GetHashCode();
            hashCode = (hashCode * 397) ^ CommandTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ CommandName.GetHashCode();
            hashCode = (hashCode * 397) ^ CommandDescription.GetHashCode();
            hashCode = (hashCode * 397) ^ SettingsTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ SettingsTypeNamespace.GetHashCode();
            hashCode = (hashCode * 397) ^ SettingsProperties.GetHashCode();
            return hashCode;
        }
    }
    public static bool operator ==(CommandInfo? left, CommandInfo? right) => Equals(left, right);
    public static bool operator !=(CommandInfo? left, CommandInfo? right) => !Equals(left, right);
}

internal class SettingsPropertyInfo : IEquatable<SettingsPropertyInfo>
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required string Aliases { get; init; }
    public required string Description { get; init; }
    public required bool IsRequired { get; init; }
    public bool Equals(SettingsPropertyInfo? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Name == other.Name
               && TypeName == other.TypeName
               && Aliases == other.Aliases
               && Description == other.Description
               && IsRequired == other.IsRequired;
    }
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((SettingsPropertyInfo)obj);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Name.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ Aliases.GetHashCode();
            hashCode = (hashCode * 397) ^ Description.GetHashCode();
            hashCode = (hashCode * 397) ^ IsRequired.GetHashCode();
            return hashCode;
        }
    }
    public static bool operator ==(SettingsPropertyInfo? left, SettingsPropertyInfo? right) => Equals(left, right);
    public static bool operator !=(SettingsPropertyInfo? left, SettingsPropertyInfo? right) => !Equals(left, right);
}
