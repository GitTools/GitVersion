using System;

namespace GitVersion.Core.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class)]
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
}