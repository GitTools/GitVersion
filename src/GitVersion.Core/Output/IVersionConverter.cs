using GitVersion.OutputVariables;

namespace GitVersion;

public interface IVersionConverter<in T> : IDisposable where T : IConverterContext
{
    public void Execute(VersionVariables variables, T context);
}
