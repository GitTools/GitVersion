using System.Linq;
using LibGit2Sharp;

namespace GitFlowVersion
{
    public class MasterVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch MasterBranch { get; set; }

        public SemanticVersion FindVersion()
        {
            var versionTag = Repository.Tags
                                       .Where(tag =>
                                           SemanticVersion.IsMajorMinorPatch(tag.Name) &&
                                           tag.IsOnBranch(MasterBranch) &&
                                           tag.IsBefore(Commit))
                                       .OrderByDescending(x => x.CommitTimeStamp())
                                       .FirstOrDefault();

            if (versionTag != null)
            {
                var version = SemanticVersion.FromMajorMinorPatch(versionTag.Name);
                version.Stage = Stage.Final;
                return version;
            }
            return new SemanticVersion
                   {
                       Stage = Stage.Final
                   };
        }
    }
}