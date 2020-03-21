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
        public IRepository Repository { get; private set; }

        /// <summary>
        /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; private set; }

        public SemanticVersion CurrentCommitTaggedVersion { get; private set; }
        public EffectiveConfiguration Configuration { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
        public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

        public GitVersionContext()
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Commit currentCommit, BranchConfig currentBranchConfig, Config configuration)
        {
            Repository = repository;
            CurrentCommit = currentCommit;
            CurrentBranch = currentBranch;

            FullConfiguration = configuration;
            Configuration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);

            CurrentCommitTaggedVersion = repository.GetCurrentCommitTaggedVersion(currentCommit, Configuration);
        }

        /*public void Update(IRepository repository, Branch currentBranch, Commit currentCommit, BranchConfig currentBranchConfig, Config configuration)
        {
            Repository = repository;
            CurrentCommit = currentCommit;
            CurrentBranch = currentBranch;

            FullConfiguration = configuration;
            Configuration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);

            CurrentCommitTaggedVersion = repository.GetCurrentCommitTaggedVersion(currentCommit, Configuration);
        }*/
    }
}
