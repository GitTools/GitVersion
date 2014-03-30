namespace Tests
{
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
                PreReleaseTag = "beta.4",
                BuildMetaData = "5.Branch.develop"
            };

            var vars = VariableProvider.GetVariablesFor(semVer);

            vars[VariableProvider.SemVer].ShouldBe("1.2.3.5-beta.4");
        }
    }
}