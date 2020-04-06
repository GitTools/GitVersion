using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.WixUpdater
{
    public interface IWixVersionFileUpdater : IDisposable
    {
        string Execute(VersionVariables variables, string workingDirectory);
    }
}
