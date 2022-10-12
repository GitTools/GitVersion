using GitVersion.Configurations;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configurations;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.Configuration;

[TestFixture]
public class ConfigExtensionsTests : TestBase
{
    [Test]
    public void GetReleaseBranchConfigReturnsAllReleaseBranches()
    {
        var config = new Model.Configurations.Configuration()
        {
            Branches = new Dictionary<string, BranchConfig>
            {
                { "foo", new BranchConfig { Name = "foo" } },
                { "bar", new BranchConfig { Name = "bar", IsReleaseBranch = true } },
                { "baz", new BranchConfig { Name = "baz", IsReleaseBranch = true } }
            }
        };

        var result = config.GetReleaseBranchConfig();

        result.Count.ShouldBe(2);
        result.ShouldNotContain(b => b.Key == "foo");
    }
}
