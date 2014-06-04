namespace GitHubFlowVersion.AcceptanceTests
{
    using GitVersion;
    using global::AcceptanceTests;
    using global::AcceptanceTests.Helpers;
    using LibGit2Sharp;
    using Shouldly;
    using Xunit;

    public class MasterTests
    {
        [Fact]
        public void GivenARepositoryWithCommitsButNoTags_VersionShouldBe_0_1()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                // Given
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();

                // When
                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("0.1.0+2");
            }
        }

        [Fact]
        public void GivenARepositoryWithCommitsButNoTagsWithDetachedHead_VersionShouldBe_0_1()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                // Given
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();

                var commit = fixture.Repository.Head.Tip;
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(commit);

                // When
                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("0.1.0+2");
            }
        }

        [Fact]
        public void GivenARepositoryWithNoTagsAndANextVersionTxtFile_VersionShouldMatchVersionTxtFile()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string ExpectedNextVersion = "1.0.0";
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();
                fixture.Repository.AddNextVersionTxtFile(ExpectedNextVersion);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.0.0+2");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndANextVersionTxtFile_VersionShouldMatchVersionTxtFile()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string ExpectedNextVersion = "1.1.0";
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(5);
                fixture.Repository.AddNextVersionTxtFile(ExpectedNextVersion);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.1.0+5");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndANextVersionTxtFileAndNoCommits_VersionShouldBeTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string ExpectedNextVersion = "1.1.0";
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.AddNextVersionTxtFile(ExpectedNextVersion);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.0.3+0");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndNoNextVersionTxtFile_VersionShouldBeTagWithBumpedPatch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(5);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.0.4+5");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndNoNextVersionTxtFileAndNoCommits_VersionShouldBeTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string TaggedVersion = "1.0.3";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.0.3+0");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndOldNextVersionTxtFile_VersionShouldBeTagWithBumpedPatch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string NextVersionTxt = "1.0.0";
                const string TaggedVersion = "1.1.0";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.MakeCommits(5);
                fixture.Repository.AddNextVersionTxtFile(NextVersionTxt);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.1.1+5");
            }
        }

        [Fact]
        public void GivenARepositoryWithTagAndOldNextVersionTxtFileAndNoCommits_VersionShouldBeTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                const string NextVersionTxt = "1.0.0";
                const string TaggedVersion = "1.1.0";
                fixture.Repository.MakeATaggedCommit(TaggedVersion);
                fixture.Repository.AddNextVersionTxtFile(NextVersionTxt);

                var result = fixture.ExecuteGitVersion();

                result.ExitCode.ShouldBe(0);
                result.OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.1.0+0");
            }
        }
    }
}