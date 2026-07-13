using GitVersion.Helpers;

namespace GitVersion.Testing;

/// <summary>
/// Creates a repo with a develop branch off main which is a single commit ahead of main
/// </summary>
public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    /// <summary>
    /// <para>Creates a repo with a develop branch off main which is a single commit ahead of main branch</para>
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
    public BaseGitFlowRepositoryFixture(Action<TestRepository> initialMainAction, string branchName = "main") :
        base(branchName) => SetupRepo(initialMainAction);

    private void SetupRepo(Action<TestRepository> initialMainAction)
    {
        var randomFile = FileSystemHelper.Path.Combine(Repository.Path, Guid.NewGuid().ToString());
        FileSystemHelper.File.WriteAllText(randomFile, string.Empty);
        Repository.Stage(randomFile);

        initialMainAction(Repository);

        Repository.Checkout(Repository.CreateBranch("develop"));
        Repository.MakeACommit();
    }
}
