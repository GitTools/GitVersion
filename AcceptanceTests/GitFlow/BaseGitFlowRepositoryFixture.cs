namespace AcceptanceTests.GitFlow
{
    using System;
    using System.IO;
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;

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

        public void AssertFullSemver(string fullSemver)
        {
            ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe(fullSemver);
        }

        public void AssertNugetPackageVersion(string nugetVersion)
        {
            var version = ExecuteGitVersion();
                version.OutputVariables[VariableProvider.NugetPackageVersion].ShouldBe(nugetVersion);
        }
    }
}
