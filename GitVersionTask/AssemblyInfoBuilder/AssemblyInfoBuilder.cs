using System.Collections.Generic;
using System.Text;
using GitVersion;

public class AssemblyInfoBuilder
{
    public CachedVersion CachedVersion;
    public AssemblyVersioningScheme AssemblyVersioningScheme;

    public string GetAssemblyInfoText()
    {
        var semanticVersion = CachedVersion.SemanticVersion;
        var vars = VariableProvider.GetVariablesFor(semanticVersion);
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


", semanticVersion.GetAssemblyVersion(AssemblyVersioningScheme), string.Format("{0}.{1}.{2}.0", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch), semanticVersion.ToString("i"),
            CachedVersion.ReleaseDate.OriginalDate.UtcDateTime.ToString("yyyy-MM-dd"),
            semanticVersion.BuildMetaData.CommitDate.UtcDateTime.ToString("yyyy-MM-dd"),
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