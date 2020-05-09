using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;
using System;

namespace GitVersion
{
    public interface IGitVersionCalculator
    {
        VersionVariables CalculateVersionVariables(bool? noCache, Config overrideConfig = null);        
    }
}
