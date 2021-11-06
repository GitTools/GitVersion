using System;

namespace GitVersion.Infrastructure;

public class Command : System.CommandLine.Command
{
    public Command(string name, string? description = null)
        : base(name, description)
    {
    }

    public Type? Parent { get; set; }
}