using System.Collections.Generic;
using System.Text;
using GitVersion;

public class AssemblyInfoBuilder
{
    public string GetAssemblyInfoText(VersionVariables vars, string assemblyName)
    {
        var assemblyInfo = string.Format(@"
using System;
using System.Reflection;

[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{1}"")]
[assembly: AssemblyInformationalVersion(""{2}"")]
[assembly: {5}.ReleaseDate(""{3}"")]

namespace {5}
{{
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class ReleaseDateAttribute : System.Attribute
    {{
        public string Date {{ get; private set; }}

        public ReleaseDateAttribute(string date)
        {{
            Date = date;
        }}
    }}

    [System.Runtime.CompilerServices.CompilerGenerated]
    static class GitVersionInformation
    {{
{4}
    }}
}}
",
        vars.AssemblySemVer,
        vars.MajorMinorPatch + ".0",
        vars.InformationalVersion,
        vars.CommitDate,
        GenerateVariableMembers(vars),
        assemblyName);

        return assemblyInfo;
    }

    string GenerateVariableMembers(IEnumerable<KeyValuePair<string, string>> vars)
    {
        var members = new StringBuilder();
        foreach (var variable in vars)
        {
            members.AppendLine(string.Format("        public static string {0} = \"{1}\";", variable.Key, variable.Value));
        }

        return members.ToString();
    }
}