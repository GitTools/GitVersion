using System;
using GitVersion.OutputVariables;

namespace GitVersion.Extensions.VersionAssemblyInfoResources
{
    public interface IAssemblyInfoFileUpdater : IDisposable
    {
        void Update(VersionVariables variables, bool ensureAssemblyInfo, string workingDirectory, params string[] assemblyInfo);
        void CommitChanges();
    }
}
