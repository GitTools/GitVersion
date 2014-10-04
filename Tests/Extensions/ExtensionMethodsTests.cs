namespace Tests.Extensions
{
    using GitVersion;
    using NUnit.Framework;

    [TestFixture]
    public partial class ExtensionMethodsTests
    {
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        public void IsOdd(int value, bool expectedValue)
        {
            Assert.AreEqual(expectedValue, value.IsOdd());
        }
    }
}