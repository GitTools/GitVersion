using System;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class VariableProviderTests
{
    [Test]
    public void DevelopBranchFormatsSemVerForCiFeed()
    {
        var semVer = new SemanticVersion
        {
            Major = 1,
            Minor = 2,
            Patch = 3,
            PreReleaseTag = "unstable.4",
            BuildMetaData = "5.Branch.develop"
        };

        semVer.BuildMetaData.ReleaseDate = new ReleaseDate
        {
            OriginalCommitSha = "originalCommitSha",
            OriginalDate = DateTimeOffset.Parse("2014-03-01 00:00:01Z"),
        };
        semVer.BuildMetaData.Sha = "commitSha";
        semVer.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");


        var vars = VariableProvider.GetVariablesFor(semVer);

        vars[VariableProvider.SemVer].ShouldBe("1.2.3.5-unstable");
    }

}