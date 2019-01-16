namespace GitTools.Tests.Git.Extensions
{
    using GitTools.Git;
    using NUnit.Framework;

    [TestFixture]
    public class AuthenticationInfoExtensionsTests
    {
        [TestCase("user", "password", null, false)]
        [TestCase(null, null, "token", false)]
        [TestCase(null, null, null, true)]
        public void TheIsEmptyMethod(string username, string password, string token, bool expectedValue)
        {
            var authenticationInfo = new AuthenticationInfo
            {
                Username = username,
                Password = password,
                Token = token
            };

            Assert.AreEqual(expectedValue, authenticationInfo.IsEmpty());
        }

        [TestCase("user", "password", null, true)]
        [TestCase(null, null, "token", false)]
        [TestCase(null, null, null, false)]
        public void TheIsUsernameAndPasswordAuthenticationMethod(string username, string password, string token, bool expectedValue)
        {
            var authenticationInfo = new AuthenticationInfo
            {
                Username = username,
                Password = password,
                Token = token
            };

            Assert.AreEqual(expectedValue, authenticationInfo.IsUsernameAndPasswordAuthentication());
        }

        [TestCase("user", "password", null, false)]
        [TestCase(null, null, "token", true)]
        [TestCase(null, null, null, false)]
        public void TheIsTokenAuthenticationMethod(string username, string password, string token, bool expectedValue)
        {
            var authenticationInfo = new AuthenticationInfo
            {
                Username = username,
                Password = password,
                Token = token
            };

            Assert.AreEqual(expectedValue, authenticationInfo.IsTokenAuthentication());
        }
    }
}
