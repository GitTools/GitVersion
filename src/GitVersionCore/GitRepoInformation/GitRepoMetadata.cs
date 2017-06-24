using GitVersion.VersionCalculation.BaseVersionCalculators;
using System.Collections.Generic;

namespace GitVersion.GitRepoInformation
{
    public class GitRepoMetadata
    {
        public GitRepoMetadata(MCurrentBranch currentBranch)
        {
            CurrentBranch = currentBranch;
        }

        public MCurrentBranch CurrentBranch { get; }
    }

    public class MCurrentBranch
    {
        public MCurrentBranch(List<MergeMessage> mergeMessages)
        {
            MergeMessages = mergeMessages;
        }

        public List<MergeMessage> MergeMessages { get; private set; }
    }
}
