using LibGit2Sharp;

namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class BaseVersion
    {
        private GitVersionContext _context;

        public BaseVersion(GitVersionContext context, string source, bool shouldIncrement, SemanticVersion semanticVersion, Commit baseVersionSource, string branchNameOverride)
        {
            Source = source;
            ShouldIncrement = shouldIncrement;
            SemanticVersion = semanticVersion;
            BaseVersionSource = baseVersionSource;
            BranchNameOverride = branchNameOverride;
            _context = context;
        }

        public string Source { get; private set; }

        public bool ShouldIncrement { get; private set; }

        public SemanticVersion SemanticVersion { get; private set; }

        public Commit BaseVersionSource { get; private set; }

        public string BranchNameOverride { get; private set; }

        public override string ToString()
        {
            return $"{Source}: {SemanticVersion.ToString("f")} with commit count source {(BaseVersionSource == null ? "External Source" : BaseVersionSource.Sha)} (Incremented: {(ShouldIncrement ? BaseVersionCalculator.MaybeIncrement(_context, this).ToString("t") : "None")})";
        }
    }
}
