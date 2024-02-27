namespace GitVersion;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CommandAttribute(string name, string description = "") : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CommandAttribute<T>(string name, string description = "") : CommandAttribute(name, description);
