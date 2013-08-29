using System.Linq;
using LibGit2Sharp;

namespace GitFlowVersion
{
    using System;

    public class MasterVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch MasterBranch { get; set; }

        public SemanticVersion FindVersion()
        {
            var message = Commit.Message;

            if (!message.StartsWith("merge "))
            {
                throw new Exception("The head of master should always be a merge commit if you follow gitflow. Please create one!");
            }

            string versionString;

            versionString = GetVersionFromMergeCommit(message);
            
            var version = SemanticVersion.FromMajorMinorPatch(versionString);

            version.Stage = Stage.Final;

            return version;
        }

        public static string GetVersionFromMergeCommit(string message)
        {
            string versionString;
            if (message.Contains("hotfix-"))
            {
                versionString = message.Substring(message.IndexOf("hotfix-") + "hotfix-".Length, 5);
            }
            else
            {
                versionString = message.Substring(message.IndexOf("release-") + "release-".Length, 5);
            }
            return versionString;
        }
    }
}