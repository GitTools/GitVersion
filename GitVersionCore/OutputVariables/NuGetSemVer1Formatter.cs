namespace GitVersion
{
    using System;
    using System.Linq;

    public static class NuGetSemVer1Formatter
    {
        public static string GetVersion(SemanticVersion semanticVersion)
        {
            var bmd = semanticVersion.BuildMetaData;
            var majorMinorPatch = string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);
            var commitsSinceTag = (bmd.CommitsSinceTag ?? 0).ToString().PadLeft(3, '0');

            //If there's no branch specified, make it a M.m.p.b format version.
            if (String.IsNullOrWhiteSpace(bmd.Branch)) return string.Format("{0}.{1}", majorMinorPatch, bmd.CommitsSinceTag ?? 0);

            var branch = bmd.Branch.ToLower();

            //The following is in heirarchical order for NuGet versioning, meaning that a hotfix version supercedes a develop version, a release version supercedes a hotfix etc.

            //At the lowest level, feature, pull request or other generic branches are denoted by a generic, lowest sort order pre-release version, labelled with the name of the branch.
            //This syntax is defined at the end of the method.

            //At this stage, the develop version is created and any other 'feature', 'pull request' or other 'generic branch' versions are superceded in NuGet.
            if (branch.Contains("develop"))
            {
                return string.Format("{0}-dev{1}", majorMinorPatch, commitsSinceTag);
            }

            //At this stage, the hotfix version is created and any other 'develop' versions (or lower) are superceded in NuGet.
            if (branch.Contains("hotfix"))
            {
                return string.Format("{0}-fix{1}", majorMinorPatch, commitsSinceTag);
            }

            //At this stage, the release candidate version is created and any other 'develop' or 'hotfix' versions (or lower) are superceded in NuGet.
            if (branch.Contains("release"))
            {
                return string.Format("{0}-rc{1}", majorMinorPatch, commitsSinceTag);
            }

            //At this stage, master's version becomes authoritative, and develop's minor revision is incremented by GitFlow.
            if (branch.Contains("master"))
            {
                return majorMinorPatch;
            }

            //Since a support branch is based off master, they should only be based on versions already released. Thus they won't ever conflict with any of the above branches' versions.
            //THis is a special case and is denoted by the version syntax below.
            if (branch.Contains("support"))
            {
                //We're already prefixing the support branch, so strip the label of any superficial 'Support' token.
                var label = GetBranchLabel(bmd.Branch).Replace("Support", "");
                return string.Format("{0}-spt{1}c{2}", majorMinorPatch, label, commitsSinceTag);
            }

            //Any other feature, pull request or other generic branches are labelled with this lowest-order naming convention.
            //Any of the above branch types will supercede this version.
            //This should suit pull requests, features and any other custom branches that may come through GitVersion.
            return string.Format("{0}-b{1}c{2}", majorMinorPatch, GetBranchLabel(bmd.Branch), commitsSinceTag);
        }

        static string GetBranchLabel(string branch)
        {
            //Get the branch label from the branch reference, e.g. feature/my-blah.122 -> my-blah.122
            var branchLabel = branch.Split('/').Last();

            //Separate out the legal tokens of the branch's label, e.g. my-blah.122 -> {my,blah,122}
            var tokens = branchLabel.Split('\\', '-', '.');

            //Capitalise each token and concatenate, e.g. {my,blah,122} -> {My,Blah,122} -> MyBlah122
            return String.Concat(tokens.Select(t => char.ToUpper(t[0]) + t.Substring(1)));
        }
    }
}