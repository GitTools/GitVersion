using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Model.Configuration;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.Configuration;

[TestFixture]
public class ConfigExtensionsTests : TestBase
{
    [Test]
    public void GetReleaseBranchConfigReturnsAllReleaseBranches()
    {
        var config = new Config()
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
