using System.IO;
using GitVersion.Command;

namespace GitVersion.Configuration
{
    [Command("config", "Manages the GitVersion configuration file.")]
    public record ConfigOptions : GitVersionOptions
    {
    }
}