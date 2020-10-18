using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class IgnoreConfigTests : TestBase
    {
        [Test]
        public void NewInstanceShouldBeEmpty()
        {
            var ignoreConfig = new IgnoreConfig();

            ignoreConfig.IsEmpty.ShouldBeTrue();
        }
    }
}
