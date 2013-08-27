using System;
using System.Linq;
using LibGit2Sharp;

namespace GitFlowVersion
{
    public class MasterVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch MasterBranch { get; set; }

        public Version FindVersion()
        {
            var versionTag = Repository.Tags
                                       .Where(tag =>
                                           tag.IsVersionable() &&
                                           tag.IsOnBranch(MasterBranch) &&
                                           tag.IsBefore(Commit))
                                       .OrderByDescending(x => x.CommitTimeStamp())
                                       .FirstOrDefault();

            Version versionFromTag;
            if (versionTag != null)
            {
                versionFromTag = versionTag.ToVersion();
            }
            else
            {
                versionFromTag = new Version(0,0);
            }
            return versionFromTag;
        }
    }
}