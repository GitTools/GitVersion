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
        var configuration = new Model.Configurations.Configuration()
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
