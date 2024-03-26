using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Testing;

/// <summary>
/// Creates a repo with a develop branch off main which is a single commit ahead of main
/// </summary>
public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    /// <summary>
    /// <para>Creates a repo with a develop branch off main which is a single commit ahead of main</para>
    /// <para>Main will be tagged with the initial version before branching develop</para>
    /// </summary>
    public BaseGitFlowRepositoryFixture(string initialVersion, string branchName = "main") :
        this(r => r.MakeATaggedCommit(initialVersion), branchName)
    {
    }

    /// <summary>
    /// <para>Creates a repo with a develop branch off main which is a single commit ahead of main</para>
    /// <para>The initial setup actions will be performed before branching develop</para>
    /// </summary>
    public BaseGitFlowRepositoryFixture(Action<IRepository> initialMainAction, string branchName = "main") :
        base(branchName) => SetupRepo(initialMainAction);

    private void SetupRepo(Action<IRepository> initialMainAction)
    {
        var randomFile = PathHelper.Combine(Repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
        File.WriteAllText(randomFile, string.Empty);
        Commands.Stage(Repository, randomFile);

        initialMainAction(Repository);

        Commands.Checkout(Repository, Repository.CreateBranch("develop"));
        Repository.MakeACommit();
    }
}
