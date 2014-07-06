namespace GitVersionTask
{
    using System.Collections.Generic;
    using System.Text;
    using GitVersion;

    public class AssemblyInfoBuilder
    {
        public SemanticVersion SemanticVersion;
        public AssemblyVersioningScheme AssemblyVersioningScheme;
        public bool AppendRevision;

        public string GetAssemblyInfoText()
        {
            var vars = VariableProvider.GetVariablesFor(SemanticVersion, AssemblyVersioningScheme, AppendRevision);
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


", vars[VariableProvider.AssemblyVersion], vars[VariableProvider.AssemblyFileVersion], SemanticVersion.ToString("i"),
                SemanticVersion.BuildMetaData.ReleaseDate.OriginalDate.UtcDateTime.ToString("yyyy-MM-dd"),
                SemanticVersion.BuildMetaData.ReleaseDate.Date.UtcDateTime.ToString("yyyy-MM-dd"),
                GenerateVariableMembers(vars));

            return assemblyInfo;
        }

        string GenerateVariableMembers(IEnumerable<KeyValuePair<string, string>> vars)
        {
            var members = new StringBuilder();
            foreach (var variable in vars)
            {
                members.AppendLine(string.Format("    public static string {0} = \"{1}\";", variable.Key, variable.Value));
            }

            return members.ToString();
        }

    }

}