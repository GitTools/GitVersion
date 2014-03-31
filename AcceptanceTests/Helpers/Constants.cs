using System;
using LibGit2Sharp;

namespace AcceptanceTests.Helpers
{
    public static class Constants
    {
        public static Signature SignatureNow()
        {
            return new Signature("A. U. Thor", "thor@valhalla.asgard.com", DateTimeOffset.Now);
        }
    }
}