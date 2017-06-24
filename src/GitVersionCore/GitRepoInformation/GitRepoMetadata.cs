using System.Collections.Generic;

namespace GitVersion.GitRepoInformation
{
    public class GitRepoMetadata
    {
        public GitRepoMetadata(List<MTag> tags, MCurrentBranch currentBranch)
        {
            Tags = tags;
            CurrentBranch = currentBranch;
        }

        public MCurrentBranch CurrentBranch { get; }
        public List<MTag> Tags { get; private set; }
    }

    public class MTag
    {
        public MTag(string sha, string friendlyName, Config config)
        {
            Sha = sha;
            Name = friendlyName;
            SemanticVersion version;
            if (SemanticVersion.TryParse(friendlyName, config.TagPrefix, out version))
            {
                Version = version;
            }
        }

        public string Sha { get; }
        public string Name { get; }
        public SemanticVersion Version { get; }
    }

    public class MCurrentBranch
    {
        public MCurrentBranch(List<MergeMessage> mergeMessages, List<MTag> tags)
        {
            MergeMessages = mergeMessages;
            Tags = tags;
        }

        public List<MergeMessage> MergeMessages { get; private set; }
        public List<MTag> Tags { get; private set; }
    }
}
