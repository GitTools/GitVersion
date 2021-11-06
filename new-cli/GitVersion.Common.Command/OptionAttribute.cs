using System;

namespace GitVersion.Command;

[AttributeUsage(AttributeTargets.Property)]
public class OptionAttribute : Attribute
{
    public string[] Aliases { get; }
    public string Description { get; }
    public bool IsRequired { get; }

    public OptionAttribute(string alias, string description = "", bool isRequired = false)
        : this(new[] { alias }, description, isRequired)
    {
    }

    public OptionAttribute(string[] aliases, string description = "", bool isRequired = false)
    {
        Aliases = aliases;
        IsRequired = isRequired;
        Description = description;
    }
}