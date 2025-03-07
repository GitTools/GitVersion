using System.IO.Abstractions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class LocalBuild(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public override bool IsDefault => true;

    protected override string EnvironmentVariable => string.Empty;
    public override bool CanApplyToCurrentContext() => true;
    public override string? GenerateSetVersionMessage(GitVersionVariables variables) => null;
    public override string[] GenerateSetParameterMessage(string name, string? value) => [];
}
