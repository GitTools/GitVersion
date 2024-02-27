namespace GitVersion;

[AttributeUsage(AttributeTargets.Property)]
public class OptionAttribute(string name, string description = "", params string[] aliases) : Attribute
{
    public string Name { get; } = name;
    public string[] Aliases { get; } = aliases;
    public string Description { get; } = description;

    public OptionAttribute(string name, string description = "")
        : this(name, description, [])
    {
    }
}
