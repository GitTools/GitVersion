namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using LibGit2Sharp;

    class VersionOnMasterFinder
    {
        public IRepository Repository;
        public DateTimeOffset OlderThan;
        public VersionPoint ExecuteOld()
        {
            var masterBranch = Repository
                .MasterBranch();
            foreach (var commit in masterBranch.CommitsPriorToThan(OlderThan))
            {
                var versionFromCommit = GetMergeOrTagMessage(commit);
                if (versionFromCommit != null)
                {
                    return new VersionPoint
                           {
                               Version = versionFromCommit,
                               Timestamp = commit.When()
                           };
                }
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


        SemanticVersion GetMergeOrTagMessage(Commit commit)
        {
            var semVerTag = Repository.SemVerTags(commit);
            if (semVerTag != null)
            {
                return semVerTag;
            }

            SemanticVersion version;
            if (MergeMessageParser.TryParse(commit.Message, out version))
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