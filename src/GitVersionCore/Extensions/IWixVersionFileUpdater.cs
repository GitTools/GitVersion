using System;
using GitVersion.OutputVariables;

namespace GitVersion.Extensions
{
    public interface IWixVersionFileUpdater : IDisposable
    {
        string Update(VersionVariables variables, string workingDirectory);
    }
}
