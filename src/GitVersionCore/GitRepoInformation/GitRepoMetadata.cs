using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitVersion.GitRepoInformation
{
    public class GitRepoMetadata
    {
        public GitRepoMetadata(
            MBranch currentBranch, MBranch master,
            List<MBranch> releaseBranches)
        {
            MasterBranch = master;
            CurrentBranch = currentBranch;
            ReleaseBranches = releaseBranches;
        }

        // Wonder if this can be 'mainline'
        public MBranch MasterBranch { get; }
        public List<MBranch> ReleaseBranches { get; }
        public MBranch CurrentBranch { get; }
    }

    public class MBranchTag
    {
        public MBranchTag(MBranch branch, MTag tag)
        {
            Branch = branch;
            Tag = tag;
        }

        public MBranch Branch { get; }
        public MTag Tag { get; }
    }

    public class MTag
    {
        public MTag(string friendlyName, MCommit commit, Config config)
        {
            Name = friendlyName;
            Commit = commit;
            SemanticVersion version;
            if (SemanticVersion.TryParse(friendlyName, config.TagPrefix, out version))
            {
                Version = version;
            }
        }

        public string Name { get; }
        public MCommit Commit { get; }
        public SemanticVersion Version { get; }
    }

    public class MBranch
    {
        public MBranch(
            string name,
            MCommit tip,
            MCommit root,
            MParent parent,
            List<MBranchTag> tags,
            List<MergeMessage> mergeMessages)
        {
            Name = name;
            Tip = tip;
            Root = root;
            Parent = parent;
            MergeMessages = mergeMessages;
            Tags = tags;
        }

        public string Name { get; }
        public MCommit Tip { get; }
        public MCommit Root { get; }
        public MParent Parent { get; }
        public List<MergeMessage> MergeMessages { get; }
        public List<MBranchTag> Tags { get; }
    }

    public class MCommit
    {
        public MCommit(Commit commit, int distanceFromTip) : this(
            commit.Sha, commit.When().DateTime, commit.Message, distanceFromTip)
        {
        }

        public MCommit(string sha, DateTime when, string message, int distanceFromTip)
        {
            Sha = sha;
            When = when;
            Message = message;
            DistanceFromTip = distanceFromTip;
        }

        public string Sha { get; }
        public string Message { get; }
        public DateTime When { get; }
        public int DistanceFromTip { get; }
    }

    public class MParent
    {
        public MParent(MCommit mergeBase)
        {
            MergeBase = mergeBase;
        }

        public MCommit MergeBase { get; }
    }
}
