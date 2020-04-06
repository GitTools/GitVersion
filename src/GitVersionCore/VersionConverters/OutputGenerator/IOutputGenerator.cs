using System;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.OutputGenerator
{
    public interface IOutputGenerator : IDisposable
    {
        void Execute(VersionVariables variables, Action<string> writter);
    }
}
