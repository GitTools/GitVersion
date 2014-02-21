namespace GitFlowVersionTask
{
    using System.Collections.Generic;
    using GitFlowVersion;

    public class AssemblyInfoBuilder
    {

        public Dictionary<string, string> Variables;
        public bool SignAssembly;
        public string AssemblyName;

        public string GetAssemblyInfoText()
        {
            var assemblyInfo = string.Format(@"
using System;
using System.Reflection;

[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{1}"")]
[assembly: AssemblyInformationalVersion(""{2}"")]
[assembly: {4}.NugetVersion(""{3}"")]

namespace {4}
{{
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NugetVersionAttribute : Attribute
    {{
        public NugetVersionAttribute(string version)
        {{
            Version = version;
        }}

        public string Version{{get;set;}}
    }}
}}
namespace {4}
{{
    [System.Runtime.CompilerServices.CompilerGenerated]
    static class GitFlowVersionInformation
    {{
        public static string AssemblyVersion = ""{0}"";
        public static string AssemblyFileVersion = ""{1}"";
        public static string AssemblyInformationalVersion = ""{2}"";
        public static string NugetVersion = ""{3}"";
        public static string SemVer = ""{5}"";
    }}
}}

", GetAssemblyVersion(), GetAssemblyFileVersion(), Variables[GitFlowVariableProvider.LongVersion], Variables[GitFlowVariableProvider.NugetVersion], AssemblyName, Variables[GitFlowVariableProvider.SemVer]);

            return assemblyInfo;
        }

        string GetAssemblyVersion()
        {
            if (SignAssembly)
            {
                // for strong named we don't want to include the patch to avoid binding redirect issues
                return string.Format("{0}.{1}.0", Variables[GitFlowVariableProvider.Major], Variables[GitFlowVariableProvider.Minor]);
            }
            // for non strong named we want to include the patch
            return string.Format("{0}.{1}.{2}", Variables[GitFlowVariableProvider.Major], Variables[GitFlowVariableProvider.Minor], Variables[GitFlowVariableProvider.Patch]);
        }

        string GetAssemblyFileVersion()
        {
            return string.Format("{0}.{1}.{2}", Variables[GitFlowVariableProvider.Major], Variables[GitFlowVariableProvider.Minor], Variables[GitFlowVariableProvider.Patch]);
        }
    }
}