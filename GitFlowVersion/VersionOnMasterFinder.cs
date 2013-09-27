namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using LibGit2Sharp;

    class VersionOnMasterFinder
    {
        public IRepository Repository;
        public DateTimeOffset OlderThan;

        SemanticVersion GetMergeOrTagMessage(Commit commit)
        {
            var semVerTag = Repository.SemVerTag(commit);
            if (semVerTag != null)
            {
                return semVerTag;
            }

            string versionString;
            if (!MergeMessageParser.TryParse(commit.Message, out versionString))
            {
                return null;
            }
            SemanticVersion version;
            if (SemanticVersionParser.TryParse(versionString, out version))
            {
                return version;
            }

            return null;
        }

        public VersionPoint Execute()
        {
            VersionPoint previous = null;
            foreach (var current in VersionsOnMaster())
            {
                if (previous != null)
                {
                    // older one is bigger
                    if (previous.Version < current.Version)
                    {
                        continue;
                    }
                    if (previous.Version.IsMinorLargerThan(current.Version))
                    {
                        return previous;
                    }
                }
                previous = current;
            }
            if (previous != null)
            {
                return previous;
            }
            return new VersionPoint
                   {
                       Version = new SemanticVersion
                                 {
                                     Major = 0,
                                     Minor = 1,
                                     Patch = 0
                                 },
                       Timestamp = DateTimeOffset.MinValue
                   };
        }

        public IEnumerable<VersionPoint> VersionsOnMaster()
        {
            var masterBranch = Repository
                .MasterBranch();
            foreach (var commit in masterBranch.CommitsPriorToThan(OlderThan))
            {
                var versionFromMergeCommit = GetMergeOrTagMessage(commit);
                if (versionFromMergeCommit != null)
                {
                    yield return new VersionPoint
                                 {
                                     Version = versionFromMergeCommit,
                                     Timestamp = commit.When()
                                 };
                }
            }
        }
    }
}