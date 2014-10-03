using System;
using System.IO;
using LibGit2Sharp;

public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    public BaseGitFlowRepositoryFixture(string initialVersion)
    {
        SetupRepo(r => r.MakeATaggedCommit(initialVersion));
    }

    public BaseGitFlowRepositoryFixture(Action<IRepository> initialMasterAction)
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
    }
}