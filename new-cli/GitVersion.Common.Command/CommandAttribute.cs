namespace GitVersion;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

    public CommandAttribute(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CommandAttribute<T> : CommandAttribute
{
    public CommandAttribute(string name, string description = "") : base(name, description)
    {
    }
}
