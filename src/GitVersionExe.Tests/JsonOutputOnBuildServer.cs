namespace GitVersionExe.Tests
{
    using GitTools.Testing;
    using NUnit.Framework;
    using Shouldly;

    public class JsonOutputOnBuildServer
    {
        [Test]
        public void BeingOnBuildServerDoesntOverrideOutputJson()
        {
            using (var fixture = new RemoteRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.2.3");
                fixture.Repository.MakeACommit();
                
                var result = GitVersionHelper.ExecuteIn(fixture.LocalRepositoryFixture.RepositoryPath, arguments: " /output json", isTeamCity: true);

                result.ExitCode.ShouldBe(0);
                result.Output.ShouldStartWith("{");
                result.Output.TrimEnd().ShouldEndWith("}");
            }
        }
    }
}