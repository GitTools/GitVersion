using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.AssemblyInfo
{
    public interface IAssemblyInfoFileUpdater : IDisposable
    {
        void Execute(VersionVariables variables, bool ensureAssemblyInfo, string workingDirectory, params string[] assemblyInfo);
    }
}
