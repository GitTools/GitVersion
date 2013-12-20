namespace GitFlowVersion
{
    using System.Text;

    public static class JsonVersionBuilder
    {
        public static string ToJson(this VersionAndBranch versionAndBranch)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLineFormat("  \"Major\":{0},", versionAndBranch.Version.Major);
            builder.AppendLineFormat("  \"Minor\":{0},", versionAndBranch.Version.Minor);
            builder.AppendLineFormat("  \"Patch\":{0},", versionAndBranch.Version.Patch);
            if (versionAndBranch.Version.Tag.HasReleaseNumber())
            {
                builder.AppendLineFormat("  \"PreReleasePartOne\":{0},", versionAndBranch.Version.Tag.ReleaseNumber());
            }
            if (versionAndBranch.Version.PreReleasePartTwo != null)
            {
                builder.AppendLineFormat("  \"PreReleasePartTwo\":{0},", versionAndBranch.Version.PreReleasePartTwo);
            }
            if (!string.IsNullOrEmpty(versionAndBranch.Version.Tag.Name))
            {
                builder.AppendLineFormat("  \"Stability\":\"{0}\",", versionAndBranch.Version.Tag.InferStability());
            }
            builder.AppendLineFormat("  \"Suffix\":\"{0}\",", versionAndBranch.Version.Suffix.JsonEncode());
            builder.AppendLineFormat("  \"LongVersion\":\"{0}\",", versionAndBranch.ToLongString().JsonEncode());
            builder.AppendLineFormat("  \"NugetVersion\":\"{0}\",", versionAndBranch.GenerateNugetVersion().JsonEncode());
            builder.AppendLineFormat("  \"ShortVersion\":\"{0}\",", versionAndBranch.ToShortString().JsonEncode());
            builder.AppendLineFormat("  \"BranchName\":\"{0}\",", versionAndBranch.BranchName.JsonEncode());
            builder.AppendLineFormat("  \"BranchType\":\"{0}\",", versionAndBranch.BranchType);
            builder.AppendLineFormat("  \"Sha\":\"{0}\"", versionAndBranch.Sha);
            builder.Append("}");
            return builder.ToString();
        }
    }
}