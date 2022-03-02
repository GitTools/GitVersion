using LibGit2Sharp;

namespace GitTools.Testing;

/// <summary>
/// Creates a repo with a develop branch off main which is a single commit ahead of main
/// </summary>
public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    /// <summary>
    /// Creates a repo with a develop branch off main which is a single commit ahead of main
    ///
    /// Main will be tagged with the initial version before branching develop
    /// </summary>
    public BaseGitFlowRepositoryFixture(string initialVersion) :
        this(r => r.MakeATaggedCommit(initialVersion))
    {
    }

    /// <summary>
    /// Creates a repo with a develop branch off main which is a single commit ahead of main
    ///
    /// The initial setup actions will be performed before branching develop
    /// </summary>
    public BaseGitFlowRepositoryFixture(Action<IRepository> initialMainAction) => SetupRepo(initialMainAction);

    private void SetupRepo(Action<IRepository> initialMainAction)
    {
        var randomFile = Path.Combine(Repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
        File.WriteAllText(randomFile, string.Empty);
        Commands.Stage(Repository, randomFile);

        initialMainAction(Repository);

        Commands.Checkout(Repository, Repository.CreateBranch("develop"));
        Repository.MakeACommit();
    }
}
