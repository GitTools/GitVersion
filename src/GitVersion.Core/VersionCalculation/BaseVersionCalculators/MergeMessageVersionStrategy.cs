using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from older commits' merge messages.
/// BaseVersionSource is the commit where the message was found.
/// Increments if PreventIncrementForMergedBranchVersion (from the branch config) is false.
/// </summary>
public class MergeMessageVersionStrategy : VersionStrategyBase
{
    private readonly ILog log;

    public MergeMessageVersionStrategy(ILog log, Lazy<GitVersionContext> versionContext) : base(versionContext) => this.log = log.NotNull();

    public override IEnumerable<BaseVersion> GetVersions()
    {
        if (Context.CurrentBranch.Commits == null || Context.CurrentCommit == null)
            return Enumerable.Empty<BaseVersion>();

        var commitsPriorToThan = Context.CurrentBranch.Commits.GetCommitsPriorTo(Context.CurrentCommit.When);
        var baseVersions = commitsPriorToThan
            .SelectMany(c =>
            {
                if (TryParse(c, Context, out var mergeMessage) &&
                    mergeMessage.Version != null &&
                    Context.FullConfiguration.IsReleaseBranch(TrimRemote(mergeMessage.MergedBranch)))
                {
                    this.log.Info($"Found commit [{Context.CurrentCommit}] matching merge message format: {mergeMessage.FormatName}");
                    var shouldIncrement = Context.Configuration.PreventIncrementForMergedBranchVersion != true;
                    return new[]
                    {
                        new BaseVersion($"{MergeMessageStrategyPrefix} '{c.Message.Trim()}'", shouldIncrement, mergeMessage.Version, c, null)
                    };
                }
                return Enumerable.Empty<BaseVersion>();
            })
            .Take(5)
            .ToList();
        return baseVersions;
    }

    public const string MergeMessageStrategyPrefix = "Merge message";

    private static bool TryParse(ICommit mergeCommit, GitVersionContext context, [NotNullWhen(true)] out MergeMessage? mergeMessage)
    {
        mergeMessage = Inner(mergeCommit, context);
        return mergeMessage != null;
    }

    private static MergeMessage? Inner(ICommit mergeCommit, GitVersionContext context)
    {
        if (mergeCommit.Parents.Count() < 2)
        {
            return null;
        }

        var mergeMessage = new MergeMessage(mergeCommit.Message, context.FullConfiguration);
        return mergeMessage;
    }

    private static string TrimRemote(string branchName) => branchName
        .RegexReplace("^refs/remotes/", string.Empty, RegexOptions.IgnoreCase)
        .RegexReplace("^origin/", string.Empty, RegexOptions.IgnoreCase);
}
