namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using LibGit2Sharp;

    public class BaseVersion
    {
        GitVersionContext _context;

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
            return string.Format(
                "{0}: {1} with commit count source {2} (Incremented: {3})",
                Source, SemanticVersion.ToString("f"),
                BaseVersionSource == null ? "External Source" : BaseVersionSource.Sha,
                ShouldIncrement ? BaseVersionCalculator.MaybeIncrement(_context, this).ToString("t") : "None");
        }
    }
}