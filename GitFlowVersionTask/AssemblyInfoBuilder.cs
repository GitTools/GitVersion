namespace GitFlowVersionTask
{
    using GitFlowVersion;
    using GitFlowVersion.VersionBuilders;

    public  class AssemblyInfoBuilder
    {

        public VersionAndBranch VersionAndBranch;
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

", GetAssemblyVersion(), GetAssemblyFileVersion(), VersionAndBranch.ToLongString(), VersionAndBranch.GenerateNugetVersion(), AssemblyName, VersionAndBranch.GenerateSemVer());
            
            return assemblyInfo;
        }

        string GetAssemblyVersion()
        {
            var semanticVersion = VersionAndBranch.Version;
            if (SignAssembly)
            {
                // for strong named we don't want to include the patch to avoid binding redirect issues
                return string.Format("{0}.{1}.0", semanticVersion.Major, semanticVersion.Minor);
            }
            // for non strong named we want to include the patch
            return string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);
        }

        string GetAssemblyFileVersion()
        {
            var semanticVersion = VersionAndBranch.Version;

            return string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);
        }
    }
}