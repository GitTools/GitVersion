using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests.Configuration;

[TestFixture]
public class ConfigurationExtensionsTests : TestBase
{
    [Test]
    public void GetReleaseBranchConfigReturnsAllReleaseBranches()
    {
        var configuration = new GitVersionConfiguration()
        {
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "foo", new BranchConfiguration { Name = "foo" } },
                { "bar", new BranchConfiguration { Name = "bar", IsReleaseBranch = true } },
                { "baz", new BranchConfiguration { Name = "baz", IsReleaseBranch = true } }
            }
        };

        var result = configuration.GetReleaseBranchConfiguration();

        result.Count.ShouldBe(2);
        result.ShouldNotContain(b => b.Key == "foo");
    }
}
