using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal class MainlineVersionCalculator : IMainlineVersionCalculator
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;
    private readonly Lazy<GitVersionContext> versionContext;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;
    private GitVersionContext Context => this.versionContext.Value;

    public MainlineVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext, IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.versionContext = versionContext.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public SemanticVersion FindMainlineModeVersion(NextVersion nextVersion)
    {
        var baseVersion = nextVersion.BaseVersion;

        if (baseVersion.SemanticVersion.PreReleaseTag.HasTag())
        {
            throw new NotSupportedException("Mainline development mode doesn't yet support pre-release tags on main");
        }

        using (this.log.IndentLog("Using mainline development mode to calculate current version"))
        {
            var mainlineVersion = baseVersion.SemanticVersion;

            // Forward merge / PR
            //          * feature/foo
            //         / |
            // main *  *
            //

            var mergeBase = baseVersion.BaseVersionSource;
            var mainline = GetMainline(baseVersion.BaseVersionSource);
            var mainlineTip = mainline.Tip;

            // when the current branch is not mainline, find the effective mainline tip for versioning the branch
            if (!Context.CurrentBranch.Equals(mainline))
            {
                (mergeBase, mainlineTip) = FindMergeBaseBeforeForwardMerge(baseVersion.BaseVersionSource, mainline);
                this.log.Info($"Current branch ({Context.CurrentBranch}) was branch from {mergeBase}");
            }

            var mainlineCommitLog = this.repositoryStore.GetMainlineCommitLog(baseVersion.BaseVersionSource, mainlineTip).ToList();
            var directCommits = new List<ICommit>(mainlineCommitLog.Count);

            if (Context.Configuration.NextVersion.IsNullOrEmpty())
            {
                // Scans commit log in reverse, aggregating merge commits
                foreach (var commit in mainlineCommitLog)
                {
                    directCommits.Add(commit);
                    if (commit.Parents.Count() > 1)
                    {
                        mainlineVersion = AggregateMergeCommitIncrement(commit, directCommits, mainlineVersion, mainline);
                    }
                }

                // This will increment for any direct commits on mainline
                mainlineVersion = IncrementForEachCommit(directCommits, mainlineVersion, mainline);
            }

            var baseVersionBuildMetaData = CreateVersionBuildMetaData(mergeBase);

            // branches other than main always get a bump for the act of branching
            if (!Context.CurrentBranch.Equals(mainline) && Context.Configuration.NextVersion.IsNullOrEmpty())
            {
                var branchIncrement = FindMessageIncrement(null, Context.CurrentCommit, mergeBase, mainlineCommitLog);
                this.log.Info($"Performing {branchIncrement} increment for current branch ");

                mainlineVersion = mainlineVersion.IncrementVersion(branchIncrement, null);
            }

            return new SemanticVersion(mainlineVersion)
            {
                PreReleaseTag = new SemanticVersionPreReleaseTag(nextVersion.IncrementedVersion.PreReleaseTag),
                BuildMetaData = baseVersionBuildMetaData
            };
        }
    }

    public SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource)
    {
        int commitsSinceTag = 0;
        if (Context.CurrentCommit != null)
        {
            var commitLogs = this.repositoryStore.GetCommitLog(baseVersionSource, Context.CurrentCommit);

            var ignore = Context.Configuration.Ignore;
            if (!ignore.IsEmpty)
            {
                var shasToIgnore = new HashSet<string>(ignore.Shas);
                commitLogs = commitLogs
                    .Where(c => ignore.Before is null || (c.When > ignore.Before && !shasToIgnore.Contains(c.Sha)));
            }
            commitsSinceTag = commitLogs.Count();

            this.log.Info($"{commitsSinceTag} commits found between {baseVersionSource} and {Context.CurrentCommit}");
        }

        var shortSha = Context.CurrentCommit?.Id.ToString(7);
        return new SemanticVersionBuildMetaData(
            baseVersionSource?.Sha,
            commitsSinceTag,
            Context.CurrentBranch.Name.Friendly,
            Context.CurrentCommit?.Sha,
            shortSha,
            Context.CurrentCommit?.When,
            Context.NumberOfUncommittedChanges);
    }

    private SemanticVersion AggregateMergeCommitIncrement(ICommit commit, List<ICommit> directCommits, SemanticVersion mainlineVersion, IBranch mainline)
    {
        // Merge commit, process all merged commits as a batch
        var mergeCommit = commit;
        var mergedHead = GetMergedHead(mergeCommit);
        var findMergeBase = this.repositoryStore.FindMergeBase(mergeCommit.Parents.First(), mergedHead);
        var findMessageIncrement = FindMessageIncrement(mergeCommit, mergedHead, findMergeBase, directCommits);

        // If this collection is not empty there has been some direct commits against main
        // Treat each commit as it's own 'release', we need to do this before we increment the branch
        mainlineVersion = IncrementForEachCommit(directCommits, mainlineVersion, mainline);
        directCommits.Clear();

        // Finally increment for the branch
        mainlineVersion = mainlineVersion.IncrementVersion(findMessageIncrement, null);
        this.log.Info($"Merge commit {mergeCommit} incremented base versions {findMessageIncrement}, now {mainlineVersion}");
        return mainlineVersion;
    }

    private IBranch GetMainline(ICommit? baseVersionSource)
    {
        if (Context.Configuration.Branches.TryGetValue(Context.CurrentBranch.Name.WithoutOrigin, out var branchConfiguration)
            && branchConfiguration.IsMainline == true)
        {
            return Context.CurrentBranch;
        }

        IDictionary<string, List<IBranch>>? mainlineBranches = null;

        if (Context.CurrentCommit != null)
        {
            mainlineBranches = this.repositoryStore.GetMainlineBranches(Context.CurrentCommit, Context.Configuration);
        }
        mainlineBranches ??= new Dictionary<string, List<IBranch>>();

        if (!mainlineBranches.Any())
        {
            var mainlineBranchConfigsString = string.Join(", ", Context.Configuration.Branches.Where(b => b.Value.IsMainline == true).Select(b => b.Key));
            throw new WarningException($"No branches can be found matching the commit {Context.CurrentCommit?.Sha} in the configured Mainline branches: {mainlineBranchConfigsString}");
        }

        var mainlineBranchNames = mainlineBranches.Values.SelectMany(branches => branches.Select(b => b.Name.Friendly));
        this.log.Info("Found possible mainline branches: " + string.Join(", ", mainlineBranchNames));

        // Find closest mainline branch
        var firstMatchingCommit = Context.CurrentBranch.Commits?.FirstOrDefault(c => mainlineBranches.ContainsKey(c.Sha));
        if (firstMatchingCommit is null)
        {
            var mainlineBranchList = mainlineBranches.Values.SelectMany(x => x).ToList();
            return FindMainlineBranch(mainlineBranchList, baseVersionSource, Context.CurrentCommit);
        }

        var possibleMainlineBranches = mainlineBranches[firstMatchingCommit.Sha];
        return FindMainlineBranch(possibleMainlineBranches, baseVersionSource, firstMatchingCommit);
    }

    private IBranch FindMainlineBranch(List<IBranch> possibleMainlineBranches, ICommit? baseVersionSource, ICommit? firstMatchingCommit)
    {
        if (possibleMainlineBranches.Count == 1)
        {
            var mainlineBranch = possibleMainlineBranches[0];
            this.log.Info($"Mainline for current branch is {mainlineBranch}");
            return mainlineBranch;
        }

        // prefer current branch, if it is a mainline branch
        if (possibleMainlineBranches.Any(Context.CurrentBranch.Equals))
        {
            this.log.Info($"Choosing {Context.CurrentBranch} as mainline because it is the current branch");
            return Context.CurrentBranch;
        }

        // prefer a branch on which the merge base was a direct commit, if there is such a branch
        var firstMatchingCommitBranch = firstMatchingCommit != null
            ? possibleMainlineBranches.Find(b => this.repositoryStore.IsCommitOnBranch(baseVersionSource, b, firstMatchingCommit))
            : null;

        if (firstMatchingCommitBranch != null)
        {
            var message = string.Format(
                "Choosing {0} as mainline because {1}'s merge base was a direct commit to {0}",
                firstMatchingCommitBranch,
                Context.CurrentBranch);
            this.log.Info(message);

            return firstMatchingCommitBranch;
        }

        var chosenMainline = possibleMainlineBranches[0];
        this.log.Info($"Multiple mainlines ({string.Join(", ", possibleMainlineBranches.Select(b => b))}) have the same merge base for the current branch, choosing {chosenMainline} because we found that branch first...");
        return chosenMainline;
    }

    /// <summary>
    /// Gets the commit on mainline at which <paramref name="mergeBase"/> was fully integrated.
    /// </summary>
    /// <param name="mainlineCommitLog">The collection of commits made directly to mainline, in reverse order.</param>
    /// <param name="mergeBase">The best possible merge base between <paramref name="mainlineTip"/> and the current commit.</param>
    /// <param name="mainlineTip">The tip of the mainline branch.</param>
    /// <returns>The commit on mainline at which <paramref name="mergeBase"/> was merged, if such a commit exists; otherwise, <paramref name="mainlineTip"/>.</returns>
    /// <remarks>
    /// This method gets the most recent commit on mainline that should be considered for versioning the current branch.
    /// </remarks>
    private ICommit? GetEffectiveMainlineTip(IEnumerable<ICommit> mainlineCommitLog, ICommit mergeBase, ICommit? mainlineTip)
    {
        // find the commit that merged mergeBase into mainline
        foreach (var commit in mainlineCommitLog)
        {
            if (Equals(commit, mergeBase) || commit.Parents.Contains(mergeBase))
            {
                this.log.Info($"Found branch merge point; choosing {commit} as effective mainline tip");
                return commit;
            }
        }

        return mainlineTip;
    }

    /// <summary>
    /// Gets the best possible merge base between the current commit and <paramref name="mainline"/> that is not the child of a forward merge.
    /// </summary>
    /// <param name="baseVersionSource">The commit that establishes the contextual base version.</param>
    /// <param name="mainline">The mainline branch.</param>
    /// <returns>The best possible merge base between the current commit and <paramref name="mainline"/> that is not the child of a forward merge.</returns>
    private (ICommit? mergeBase, ICommit? mainlineTip) FindMergeBaseBeforeForwardMerge(ICommit? baseVersionSource, IBranch mainline)
    {
        var mainlineTipCurrent = mainline.Tip;
        var mainlineCommitLog = this.repositoryStore.GetMainlineCommitLog(baseVersionSource, mainlineTipCurrent).ToList();

        // find the mainline commit effective for versioning the current branch
        if (Context.CurrentCommit != null && mainlineTipCurrent != null)
        {
            var mergeBase = this.repositoryStore.FindMergeBase(Context.CurrentCommit, mainlineTipCurrent);
            if (mergeBase != null)
            {
                var mainlineTip = GetEffectiveMainlineTip(mainlineCommitLog, mergeBase, mainlineTipCurrent);

                // detect forward merge and rewind mainlineTip to before it
                if (Equals(mergeBase, Context.CurrentCommit) && !mainlineCommitLog.Contains(mergeBase))
                {
                    var mainlineTipPrevious = mainlineTip?.Parents.FirstOrDefault();
                    if (mainlineTipPrevious != null)
                    {
                        this.log.Info($"Detected forward merge at {mainlineTip}; rewinding mainline to previous commit {mainlineTipPrevious}");

                        // re-do mergeBase detection before the forward merge
                        mergeBase = this.repositoryStore.FindMergeBase(Context.CurrentCommit, mainlineTipPrevious);
                        if (mergeBase != null)
                            mainlineTip = GetEffectiveMainlineTip(mainlineCommitLog, mergeBase, mainlineTipPrevious);
                    }
                }

                return (mergeBase, mainlineTip);
            }
        }
        return (null, null);
    }

    private SemanticVersion IncrementForEachCommit(IEnumerable<ICommit> directCommits, SemanticVersion mainlineVersion, IBranch mainline)
    {
        foreach (var directCommit in directCommits)
        {
            var directCommitIncrement = this.incrementStrategyFinder.GetIncrementForCommits(
                majorVersionBumpMessage: Context.Configuration.MajorVersionBumpMessage,
                minorVersionBumpMessage: Context.Configuration.MinorVersionBumpMessage,
                patchVersionBumpMessage: Context.Configuration.PatchVersionBumpMessage,
                noBumpMessage: Context.Configuration.NoBumpMessage,
                commits: new[] { directCommit }
            ) ?? FindDefaultIncrementForBranch(Context, mainline);
            mainlineVersion = mainlineVersion.IncrementVersion(directCommitIncrement, null);
            this.log.Info($"Direct commit on main {directCommit} incremented base versions {directCommitIncrement}, now {mainlineVersion}");
        }

        return mainlineVersion;
    }

    private VersionField FindMessageIncrement(ICommit? mergeCommit, ICommit? mergedHead, ICommit? findMergeBase, List<ICommit> commitLog)
    {
        var commits = this.repositoryStore.GetMergeBaseCommits(mergeCommit, mergedHead, findMergeBase);
        commitLog.RemoveAll(c => commits.Any(c1 => c1.Sha == c.Sha));

        var messageIncrement = this.incrementStrategyFinder.GetIncrementForCommits(
            majorVersionBumpMessage: Context.Configuration.MajorVersionBumpMessage,
            minorVersionBumpMessage: Context.Configuration.MinorVersionBumpMessage,
            patchVersionBumpMessage: Context.Configuration.PatchVersionBumpMessage,
            noBumpMessage: Context.Configuration.NoBumpMessage,
            commits: commits
        );
        return messageIncrement ?? TryFindIncrementFromMergeMessage(mergeCommit);
    }

    private VersionField TryFindIncrementFromMergeMessage(ICommit? mergeCommit)
    {
        if (mergeCommit != null)
        {
            var mergeMessage = new MergeMessage(mergeCommit.Message, Context.Configuration);
            var branchName = mergeMessage.MergedBranch;
            if (branchName != null)
            {
                var configuration = Context.Configuration.GetBranchConfiguration(branchName);
                if (configuration.Increment != IncrementStrategy.Inherit)
                {
                    return configuration.Increment.ToVersionField();
                }
            }
        }

        return FindDefaultIncrementForBranch(Context);
    }

    private static VersionField FindDefaultIncrementForBranch(GitVersionContext context)
        => FindDefaultIncrementForBranch(context, context.CurrentBranch);

    private static VersionField FindDefaultIncrementForBranch(GitVersionContext context, IBranch branch)
    {
        var increment = context.Configuration.GetEffectiveConfiguration(branch).Increment;
        if (increment == IncrementStrategy.Inherit) increment = IncrementStrategy.Patch;
        return increment.ToVersionField();
    }

    private static ICommit GetMergedHead(ICommit mergeCommit)
    {
        var parents = mergeCommit.Parents.Skip(1).ToList();
        if (parents.Count > 1)
            throw new NotSupportedException("Mainline development does not support more than one merge source in a single commit yet");
        return parents.Single();
    }
}
