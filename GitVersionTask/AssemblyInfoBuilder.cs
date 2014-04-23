namespace GitVersionTask
{
    using System.Text;
    using GitVersion;

    public class AssemblyInfoBuilder
    {
        public SemanticVersion SemanticVersion;
        public bool SignAssembly;
        public bool AppendRevision;

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
sealed class ReleaseDateAttribute : System.Attribute
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


", GetAssemblyVersion(), GetAssemblyFileVersion(), SemanticVersion.ToString("i"),
                SemanticVersion.BuildMetaData.OriginalReleaseDate.Value.UtcDateTime.ToString("yyyy-MM-dd"),
                SemanticVersion.BuildMetaData.ReleaseDate.Value.UtcDateTime.ToString("yyyy-MM-dd"),
                GenerateVariableMembers());

            return assemblyInfo;
        }

        string GenerateVariableMembers()
        {
            var members = new StringBuilder();
            foreach (var variable in VariableProvider.GetVariablesFor(SemanticVersion))
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
                return string.Format("{0}.{1}.0", SemanticVersion.Major, SemanticVersion.Minor);
            }
            // for non strong named we want to include the patch
            return GetAssemblyFileVersion();
        }

        string GetAssemblyFileVersion()
        {
            if (AppendRevision && SemanticVersion.BuildMetaData.Branch == "master")
            {
                if (SemanticVersion.BuildMetaData.CommitsSinceTag != null)
                {
                    return string.Format("{0}.{1}.{2}.{3}", SemanticVersion.Major, SemanticVersion.Minor, SemanticVersion.Patch, SemanticVersion.BuildMetaData.CommitsSinceTag);   
                }
            }
            return string.Format("{0}.{1}.{2}", SemanticVersion.Major, SemanticVersion.Minor, SemanticVersion.Patch);
        }
    }


}