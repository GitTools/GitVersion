using System;
using LibGit2Sharp;

namespace GitTools.Testing
{
    /// <summary>
    /// Static helper class for generating data git needs, like signatures
    /// </summary>
    public static class Generate
    {
        /// <summary>
        /// Create a libgit2sharp signature at VirtualTime.Now
        /// </summary>
        public static Signature SignatureNow()
        {
            var dateTimeOffset = VirtualTime.Now;
            return Signature(dateTimeOffset);
        }

        /// <summary>
        /// Creates a libgit2sharp signature at the specified time
        /// </summary>
        public static Signature Signature(DateTimeOffset dateTimeOffset)
        {
            return new Signature("A. U. Thor", "thor@valhalla.asgard.com", dateTimeOffset);
        }
    }
}