using GitVersion.Model.Configuration;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        /// <summary>
        /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; }

        public SemanticVersion CurrentCommitTaggedVersion { get; }
        public EffectiveConfiguration Configuration { get; }
        public Branch CurrentBranch { get; }
        public Commit CurrentCommit { get; }
        public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

        public GitVersionContext()
        {
        }

        public GitVersionContext(Branch currentBranch, Commit currentCommit,
            Config configuration, EffectiveConfiguration effectiveConfiguration, SemanticVersion currentCommitTaggedVersion)
        {
            CurrentCommit = currentCommit;
            CurrentBranch = currentBranch;

            FullConfiguration = configuration;
            Configuration = effectiveConfiguration;

            CurrentCommitTaggedVersion = currentCommitTaggedVersion;
        }
    }
}
