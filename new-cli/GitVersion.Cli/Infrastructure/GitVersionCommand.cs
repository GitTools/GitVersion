using System;

namespace GitVersion.Cli.Infrastructure
{
    public class GitVersionCommand : System.CommandLine.Command
    {
        public GitVersionCommand(string name, string? description = null)
            : base(name, description)
        {
        }

        public Type? Parent { get; set; }
    }
}