using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.GitVersionInfo
{
    public interface IGitVersionInfoGenerator : IDisposable
    {
        void Execute(VersionVariables variables, FileWriteInfo writeInfo);
    }
}
