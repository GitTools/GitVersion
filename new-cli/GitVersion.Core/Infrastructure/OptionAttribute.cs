using System;

namespace GitVersion.Infrastructure
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
        public string[] Aliases { get; }
        public string Description { get; }
        public bool Required { get; }

        public OptionAttribute(string alias, string description = "", bool required = false)
            : this(new[] { alias }, description, required)
        {
        }

        public OptionAttribute(string[] aliases, string description = "", bool required = false)
        {
            Aliases = aliases;
            Required = required;
            Description = description;
        }
    }
}