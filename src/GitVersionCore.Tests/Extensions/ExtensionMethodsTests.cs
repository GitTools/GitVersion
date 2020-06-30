using GitVersion.Extensions;
using NUnit.Framework;

namespace GitVersionCore.Tests.Extensions
{
    [TestFixture]
    public class ExtensionMethodsTests
    {
        [TestCase("develop", "develop", true)]
        [TestCase("develop", "master", false)]
        [TestCase("/refs/head/develop", "develop", true)]
        [TestCase("/refs/head/master", "develop", false)]
        [TestCase("superdevelop", "develop", false)]
        [TestCase("/refs/head/superdevelop", "develop", false)]
        public void TheIsBranchMethod(string input1, string input2, bool expectedOutput)
        {
            var actualOutput = input1.IsBranch(input2);

            Assert.AreEqual(expectedOutput, actualOutput);
        }
    }
}
