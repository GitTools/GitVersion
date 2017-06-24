using System.Collections.Generic;

namespace GitVersion.GitRepoInformation
{
    public class GitRepoMetadata
    {
        public GitRepoMetadata(
            List<MTag> tags, MBranch currentBranch, MBranch master,
            List<MBranch> releaseBranches)
        {
            Tags = tags;
            MasterBranch = master;
            CurrentBranch = currentBranch;
            ReleaseBranches = releaseBranches;
        }

        // Wonder if this can be 'mainline'
        public MBranch MasterBranch { get; }
        public List<MBranch> ReleaseBranches { get; }
        public MBranch CurrentBranch { get; }
        public List<MTag> Tags { get; private set; }
    }

    public class MTag
    {
        public MTag(string sha, string friendlyName, Config config, bool createdAfterHead)
        {
            Sha = sha;
            Name = friendlyName;
            CreatedAfterHead = createdAfterHead;
            SemanticVersion version;
            if (SemanticVersion.TryParse(friendlyName, config.TagPrefix, out version))
            {
                Version = version;
            }
        }

        public string Sha { get; }
        public string Name { get; }
        public bool CreatedAfterHead { get; }
        public SemanticVersion Version { get; }
    }

    public class MBranch
    {
        public MBranch(
            string name,
            string tipSha,
            MParent parent,
            List<MTag> tags,
            List<MergeMessage> mergeMessages)
        {
            Name = name;
            TipSha = tipSha;
            Parent = parent;
            MergeMessages = mergeMessages;
            Tags = tags;
        }

        public string Name { get; }
        public string TipSha { get; }
        public MParent Parent { get; }
        public List<MergeMessage> MergeMessages { get; }
        public List<MTag> Tags { get; }
    }

    public class MParent
    {
        public MParent(string mergeBase)
        {
            MergeBase = mergeBase;
        }

        public string MergeBase { get; }
    }
}
