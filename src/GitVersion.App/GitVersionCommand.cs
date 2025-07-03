using System.ComponentModel;
using Spectre.Console.Cli;

namespace GitVersion;

/// <summary>
/// Main GitVersion command with POSIX compliant options
/// </summary>
[Description("Generate version information based on Git repository")]
internal class GitVersionCommand : Command<GitVersionSettings>
{
    public override int Execute(CommandContext context, GitVersionSettings settings) => 0;
}