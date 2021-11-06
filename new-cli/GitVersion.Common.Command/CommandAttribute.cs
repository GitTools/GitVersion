using System;

namespace GitVersion.Command;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public Type? Parent { get; }
    public string? Description { get; }

    public CommandAttribute(string name, string description = "") : this(name, null, description)
    {
        Name = name;
        Description = description;
    }

    public CommandAttribute(string name, Type? parent, string? description = "")
    {
        Name = name;
        Parent = parent;
        Description = description;
    }
}