using GitVersion.Configuration;
using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public IRepository Repository { get; }
        /// <summary>
        /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; }
        public SemanticVersion CurrentCommitTaggedVersion { get; }
        public EffectiveConfiguration Configuration { get; }
        public Branch CurrentBranch { get; }
        public Commit CurrentCommit { get; }
        public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

        public GitVersionContext(IRepository repository, Branch currentBranch, Commit currentCommit, BranchConfig currentBranchConfig, Config configuration)
        {
            Repository = repository;
            CurrentCommit = currentCommit;
            CurrentBranch = currentBranch;

            FullConfiguration = configuration;
            Configuration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);

            CurrentCommitTaggedVersion = repository.GetCurrentCommitTaggedVersion(currentCommit, Configuration);
        }
    }
}
