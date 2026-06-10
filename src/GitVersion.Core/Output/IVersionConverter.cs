using GitVersion.OutputVariables;

namespace GitVersion;

/// <summary>Converts <see cref="GitVersionVariables"/> to a specific output format (e.g. JSON, environment variables, build-server properties).</summary>
public interface IVersionConverter<in T> : IDisposable where T : IConverterContext
{
    /// <summary>Performs the conversion of <paramref name="variables"/> using the supplied <paramref name="context"/>.</summary>
    void Execute(GitVersionVariables variables, T context);
}
