using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using GitVersion;

public class ExecutionResults
{
    public ExecutionResults(int exitCode, string output, string logContents)
    {
        ExitCode = exitCode;
        Output = output;
        Log = logContents;
    }

    public int ExitCode { get; private set; }
    public string Output { get; private set; }
    public string Log { get; private set; }

    public virtual VersionVariables OutputVariables
    {
        get
        {
            var outputVariables = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Output);
            var type = typeof(VersionVariables);
            var ctor = type.GetConstructors().Single();
            var ctorArgs = ctor.GetParameters()
                .Select(p => outputVariables.Single(v => v.Key.ToLower() == p.Name.ToLower()).Value)
                .Cast<object>()
                .ToArray();
            return (VersionVariables) Activator.CreateInstance(type, ctorArgs);
        }
    }
}