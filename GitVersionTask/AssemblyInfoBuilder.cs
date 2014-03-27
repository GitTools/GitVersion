namespace GitVersionTask
{
    using GitVersion;

    public class AssemblyInfoBuilder
    {
        public VersionAndBranchAndDate VersionAndBranch;
        public bool SignAssembly;
        public string GetAssemblyInfoText()
        {
            var assemblyInfo = string.Format(@"
using System;
using System.Reflection;

[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{1}"")]
[assembly: AssemblyInformationalVersion(""{2}"")]
[assembly: NugetVersion(""{3}"")]
[assembly: ReleaseDate(""{5}"", ""{6}"")]

[System.Runtime.CompilerServices.CompilerGenerated]
class NugetVersionAttribute : Attribute
{{
    public NugetVersionAttribute(string version)
    {{
        Version = version;
    }}

    public string Version{{get;set;}}
}}

[System.Runtime.CompilerServices.CompilerGenerated]
class ReleaseDateAttribute : System.Attribute
{{
    public string OriginalDate {{ get; private set; }}
    public string Date {{ get; private set; }}

    public ReleaseDateAttribute(string originalDate, string date)
    {{
        OriginalDate = date;
        Date = date;
    }}
}}

[System.Runtime.CompilerServices.CompilerGenerated]
static class GitVersionInformation
{{
    public static string AssemblyVersion = ""{0}"";
    public static string AssemblyFileVersion = ""{1}"";
    public static string AssemblyInformationalVersion = ""{2}"";
    public static string NugetVersion = ""{3}"";
    public static string SemVer = ""{4}"";
}}


", GetAssemblyVersion(), GetAssemblyFileVersion(), VersionAndBranch.ToLongString(), VersionAndBranch.GenerateNugetVersion(), VersionAndBranch.GenerateSemVer(),
 VersionAndBranch.ReleaseDate.OriginalDate.UtcDateTime.ToString("yyyy-MM-dd"), VersionAndBranch.ReleaseDate.Date.UtcDateTime.ToString("yyyy-MM-dd"));

            return assemblyInfo;
        }

        string GetAssemblyVersion()
        {
            if (SignAssembly)
            {
                // for strong named we don't want to include the patch to avoid binding redirect issues
                return string.Format("{0}.{1}.0", VersionAndBranch.Version.Major, VersionAndBranch.Version.Minor);
            }
            // for non strong named we want to include the patch
            return string.Format("{0}.{1}.{2}", VersionAndBranch.Version.Major, VersionAndBranch.Version.Minor, VersionAndBranch.Version.Patch);
        }

        string GetAssemblyFileVersion()
        {
            return string.Format("{0}.{1}.{2}", VersionAndBranch.Version.Major, VersionAndBranch.Version.Minor, VersionAndBranch.Version.Patch);
        }
    }
}