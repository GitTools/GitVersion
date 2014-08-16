namespace Tests.Extensions
{
    using GitVersion;
    using NUnit.Framework;

    public partial class ExtensionMethodsTests
    {
        [TestCase("develop", "refs/heads/develop")]
        [TestCase("master", "refs/heads/master")]
        [TestCase("pr/30", "refs/pull/30/head")]
        public void GetCanonicalBranchName(string branchName, string expectedName)
        {
            Assert.AreEqual(expectedName, branchName.GetCanonicalBranchName());
        }
    }
}