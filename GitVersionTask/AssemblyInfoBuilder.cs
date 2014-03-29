namespace GitVersionTask
{
    using System.Text;
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
[assembly: ReleaseDate(""{3}"", ""{4}"")]

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
{5}
}}


", GetAssemblyVersion(), GetAssemblyFileVersion(), VersionAndBranch.ToLongString(), 
 VersionAndBranch.ReleaseDate.OriginalDate.UtcDateTime.ToString("yyyy-MM-dd"), 
 VersionAndBranch.ReleaseDate.Date.UtcDateTime.ToString("yyyy-MM-dd"),
 GenerateVariableMembers());

            return assemblyInfo;
        }

        string GenerateVariableMembers()
        {
            var members = new StringBuilder();
            foreach (var variable in VersionAndBranch.ToKeyValue())
            {
                members.AppendLine(string.Format("    public static string {0} = \"{1}\";", variable.Key, variable.Value));
            }

            return members.ToString();
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