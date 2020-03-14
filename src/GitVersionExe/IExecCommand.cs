using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IExecCommand
    {
        void Execute(VersionVariables variables);
    }
}
