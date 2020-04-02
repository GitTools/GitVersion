using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.VersionAssemblyInfoResources
{
    public interface IAssemblyInfoFileUpdater : IDisposable
    {
        void Update(VersionVariables variables, bool ensureAssemblyInfo, string workingDirectory, params string[] assemblyInfo);
        void CommitChanges();
    }
}
