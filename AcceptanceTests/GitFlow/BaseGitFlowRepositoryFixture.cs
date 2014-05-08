namespace AcceptanceTests.GitFlow
{
    using System;
    using System.IO;
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;

    public class BaseGitFlowRepositoryFixture : IDisposable
    {
        public string RepositoryPath;
        public Repository Repository;

        public BaseGitFlowRepositoryFixture(string initialVersion)
        {
            SetupRepo(r => r.MakeATaggedCommit(initialVersion));
        }

        public BaseGitFlowRepositoryFixture(Action<Repository> initialMasterAction)
        {
            SetupRepo(initialMasterAction);
        }

        void SetupRepo(Action<Repository> initialMasterAction)
        {
            RepositoryPath = PathHelper.GetTempPath();
            Repository.Init(RepositoryPath);
            Console.WriteLine("Created git repository at {0}", RepositoryPath);

            Repository = new Repository(RepositoryPath);
            Repository.Config.Set("user.name", "Test");
            Repository.Config.Set("user.email", "test@email.com");

            var randomFile = Path.Combine(Repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
            File.WriteAllText(randomFile, string.Empty);
            Repository.Index.Stage(randomFile);

            initialMasterAction(Repository);


            Repository.CreateBranch("develop").Checkout();

        }

        public void AssertFullSemver(string fullSemver)
        {
            ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe(fullSemver);
        }

        public void Dispose()
        {
            Repository.Dispose();
            try
            {
                DirectoryHelper.DeleteDirectory(RepositoryPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath, e.Message);
            }
        }

        public ExecutionResults ExecuteGitVersion()
        {
            return GitVersionHelper.ExecuteIn(RepositoryPath);
        }
    }
}