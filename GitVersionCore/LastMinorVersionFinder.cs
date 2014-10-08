using System.Linq;
using LibGit2Sharp;

namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public class LastMinorVersionFinder
    {
        public static DateTimeOffset Execute(IRepository repo, Commit commit)
        {
            // Release/Develop = current
            // Hotfix/Master/Support = walk back current branch until previous commits till a merge commit (or tag) has a patch with a 0
            if (
                repo.Head.IsMaster() ||
                repo.Head.IsHotfix() ||
                repo.Head.IsSupport()
                )
            {
                var fromTag = GetTimeStampFromTag(repo, commit);
                
                if (fromTag != DateTimeOffset.MinValue)
                {
                    return fromTag;
                }
            }
            return commit.When();
        }


        static DateTimeOffset GetTimeStampFromTag(IRepository repository, Commit targetCommit)
        {
            var allMajorMinorTags = repository.Tags
                .Where(x => ShortVersionParser.IsMajorMinor(x.Name))
                .ToDictionary(x => x.PeeledTarget(), x => x);
            var olderThan = targetCommit.When();
            foreach (var commit in repository.Head.Commits.Where(x => x.When() <= olderThan))
            {
                if (IsMajorMinor(commit, allMajorMinorTags))
                {
                    return commit.When();
                }
            }
            return DateTimeOffset.MinValue;
        }

        static bool IsMajorMinor(Commit commit, Dictionary<GitObject, Tag> allMajorMinorTags)
        {
            ShortVersion version;
            if (MergeMessageParser.TryParse(commit, out version))
            {
                if (version.Patch == 0)
                {
                    return true;
                }
            }
            return allMajorMinorTags.ContainsKey(commit);
        }
    }
}