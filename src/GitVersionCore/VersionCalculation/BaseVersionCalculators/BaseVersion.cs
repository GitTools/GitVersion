using LibGit2Sharp;

namespace GitVersion.VersionCalculation
{
    public class BaseVersion
    {
        public BaseVersion(string source, bool shouldIncrement, SemanticVersion semanticVersion, Commit baseVersionSource, string branchNameOverride)
        {
            Source = source;
            ShouldIncrement = shouldIncrement;
            SemanticVersion = semanticVersion;
            BaseVersionSource = baseVersionSource;
            BranchNameOverride = branchNameOverride;
        }

        public string Source { get; }

        public bool ShouldIncrement { get; }

        public SemanticVersion SemanticVersion { get; }

        public Commit BaseVersionSource { get; }

        public string BranchNameOverride { get; }

        public override string ToString()
        {
            var externalSource = BaseVersionSource == null ? "External Source" : BaseVersionSource.Sha;
            return $"{Source}: {SemanticVersion.ToString("f")} with commit count source {externalSource}";
        }
    }
}
