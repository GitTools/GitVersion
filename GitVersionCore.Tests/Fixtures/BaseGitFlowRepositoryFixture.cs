using System;
using System.IO;
using GitVersion;
using LibGit2Sharp;

/// <summary>
/// Creates a repo with a develop branch off master which is a single commit ahead of master
/// </summary>
public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    /// <summary>
    /// Creates a repo with a develop branch off master which is a single commit ahead of master
    /// 
    /// Master will be tagged with the initial version before branching develop
    /// </summary>
    public BaseGitFlowRepositoryFixture(string initialVersion) : base(new Config())
    {
        SetupRepo(r => r.MakeATaggedCommit(initialVersion));
    }

    /// <summary>
    /// Creates a repo with a develop branch off master which is a single commit ahead of master
    /// 
    /// The initial setup actions will be performed before branching develop
    /// </summary>
    public BaseGitFlowRepositoryFixture(Action<IRepository> initialMasterAction) : base(new Config())
    {
        SetupRepo(initialMasterAction);
    }

    void SetupRepo(Action<IRepository> initialMasterAction)
    {
        var randomFile = Path.Combine(Repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
        File.WriteAllText(randomFile, string.Empty);
        Repository.Index.Stage(randomFile);

        initialMasterAction(Repository);

        Repository.CreateBranch("develop").Checkout();
        Repository.MakeACommit();
    }
}