using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.WixUpdater
{
    public interface IWixVersionFileUpdater : IDisposable
    {
        string Update(VersionVariables variables, string workingDirectory);
    }
}
