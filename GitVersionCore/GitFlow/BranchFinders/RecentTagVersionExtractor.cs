namespace GitVersion
{
    using System.Collections.Generic;
    using LibGit2Sharp;

    class RecentTagVersionExtractor
    {
        internal static SemanticVersionPreReleaseTag RetrieveMostRecentOptionalTagVersion(IRepository repository, ShortVersion matchVersion, IEnumerable<Commit> take)
        {
            Commit first = null;
            foreach (var commit in take)
            {
                if (first == null)
                {
                    first = commit;
                }
                foreach (var tag in repository.TagsByDate(commit))
                {
                    SemanticVersion version;
                    if (!SemanticVersion.TryParse(tag.Name, out version))
                    {
                        continue;
                    }

                    if (matchVersion.Major == version.Major && 
                        matchVersion.Minor == version.Minor && 
                        matchVersion.Patch == version.Patch)
                    {
                        var preReleaseTag = version.PreReleaseTag;

                        //If the tag is on the eact commit then dont bump the PreReleaseTag 
                        if (first != commit)
                        {
                            preReleaseTag.Number++;
                        }
                        return preReleaseTag;
                    }
                }
            }

            return null;
        }


    }
}