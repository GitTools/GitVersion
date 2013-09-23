namespace GitFlowVersion
{
    using System;
    using LibGit2Sharp;

    class VersionOnMasterFinder
    {
        public IRepository Repository;

        public VersionPoint Execute(DateTimeOffset olderThan)
        {
            var masterBranch = Repository
                .MasterBranch();
            foreach (var commit in masterBranch.CommitsPriorToThan(olderThan))
            {
                var message = GetMergeOrTagMessage(commit);
                if (message != null)
                {
                    var versionFromMergeCommit = MergeMessageParser.GetVersionFromMergeCommit(message);
                    return new VersionPoint
                           {
                               Version = SemanticVersion.FromMajorMinorPatch(versionFromMergeCommit),
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


        string GetMergeOrTagMessage(Commit commit)
        {
            var semVerTag = Repository.SemVerTags(commit);
            if (semVerTag != null)
            {
                return semVerTag.Name;
            }

            if (commit.Message.StartsWith("merge"))
            {
                return commit.Message;
            }

            return null;
        }

        //public static VersionPoint MasterVersionPriorToNew(this IRepository repository, DateTimeOffset olderThan)
        //{
        //    VersionPoint previous = null;
        //    foreach (var current in repository.VersionsOnMaster(olderThan))
        //    {
        //        if (previous != null)
        //        {
        //            if (previous.Version.IsMinorLargerThan(previous.Version))
        //            {
        //                return previous;
        //            }
        //        }
        //        previous = current;
        //    }
        //    if (previous != null)
        //    {
        //        return previous;
        //    }
        //    return new VersionPoint
        //    {
        //        Version = new SemanticVersion
        //        {
        //            Major = 0,
        //            Minor = 1,
        //            Patch = 0
        //        },
        //        Timestamp = DateTimeOffset.MinValue
        //    };
        //}
    }
}