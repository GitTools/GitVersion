using System;
using System.IO;
using GitVersion;
using LibGit2Sharp;

public class BaseGitFlowRepositoryFixture : EmptyRepositoryFixture
{
    public BaseGitFlowRepositoryFixture(string initialVersion) : base(new Config())
    {
        SetupRepo(r => r.MakeATaggedCommit(initialVersion));
    }

    public BaseGitFlowRepositoryFixture(Action<IRepository> initialMasterAction) : base(new Config())
    {
        SetupRepo(initialMasterAction);
    }

    void SetupRepo(Action<IRepository> initialMasterAction)
    {
        var randomFile = Path.Combine(Repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
        File.WriteAllText(randomFile, string.Empty);
        Repository.Stage(randomFile);

        initialMasterAction(Repository);

        var branch = Repository.CreateBranch("develop");
        Repository.Checkout(branch);
    }
}