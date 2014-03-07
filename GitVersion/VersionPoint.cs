namespace GitVersion
{
    using System;

    class VersionPoint
    {
        public int Major;
        public int Minor;
        public DateTimeOffset Timestamp;
        public string CommitSha;
    }
}
