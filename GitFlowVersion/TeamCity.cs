namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public static class TeamCity
    {

        public static bool IsRunningInBuildAgent()
        {
            var isRunningInBuildAgent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            if (isRunningInBuildAgent)
            {
                Logger.WriteInfo("Executing inside a TeamCityVersionBuilder build agent");
            }
            return isRunningInBuildAgent;
        }

        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
        {
            if (!IsRunningInBuildAgent())
            {
                yield break;
            }
            var semanticVersion = versionAndBranch.Version;

            yield return TeamCityVersionBuilder.GenerateBuildVersion(versionAndBranch);
            yield return GenerateBuildParameter("Major", semanticVersion.Major.ToString());
            yield return GenerateBuildParameter("Minor", semanticVersion.Minor.ToString());
            yield return GenerateBuildParameter("Patch", semanticVersion.Patch.ToString());
            yield return GenerateBuildParameter("Stability", semanticVersion.Stability.ToString());
            yield return GenerateBuildParameter("PreReleaseNumber", semanticVersion.PreReleasePartOne.ToString());
            yield return GenerateBuildParameter("Version", TeamCityVersionBuilder.CreateVersionString(versionAndBranch));
            yield return GenerateBuildParameter("NugetVersion", NugetVersionBuilder.GenerateNugetVersion(versionAndBranch));
        }

        static string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }

        public static void NormalizeGitDirectory(string gitDirectory)
        {
            using (var repo = new Repository(gitDirectory))
            {
                EnsureOnlyOneRemoteIsDefined(repo);
                CreateMissingLocalBranchesFromRemoteTrackingOnes(repo);

                if (!repo.Info.IsHeadDetached)
                {
                    return;
                }

                CreateFakeBranchPointingAtThePullRequestTip(repo);
            }
        }

        private static void CreateFakeBranchPointingAtThePullRequestTip(Repository repo)
        {
            var remote = repo.Network.Remotes.Single();
            var remoteTips = repo.Network.ListReferences(remote);

            var headTipSha = repo.Head.Tip.Sha;

            var refs = remoteTips.Where(r => r.TargetIdentifier == headTipSha).ToList();

            if (refs.Count == 0)
            {
                throw new ErrorException(
                    string.Format("Couldn't find any remote tips from remote '{0}' pointing at the commit '{1}'.", remote.Url, headTipSha));
            }

            if (refs.Count > 1)
            {
                throw new ErrorException(
                    string.Format("Found more than one remote tip from remote '{0}' pointing at the commit '{1}'. "
                            + "Unable to determine which one to use ({2}).",
                            remote.Url, headTipSha, string.Join(", ", refs.Select(r => r.CanonicalName))));
            }

            var canonicalName = refs[0].CanonicalName;
            if (!canonicalName.StartsWith("refs/pull/"))
            {
                throw new ErrorException(
                    string.Format("Remote tip '{0}' from remote '{1}' doesn't look like a valid pull request.", canonicalName, remote.Url));
            }

            var fakeBranchName = canonicalName.Replace("refs/pull/", "refs/heads/pull/");
            repo.Refs.Add(fakeBranchName, new ObjectId(headTipSha));

            repo.Checkout(fakeBranchName);
        }

        private static void CreateMissingLocalBranchesFromRemoteTrackingOnes(Repository repo)
        {
            var remoteName = repo.Network.Remotes.Single().Name;
            var prefix = string.Format("refs/remotes/{0}/", remoteName);

            foreach (var remoteTrackingReference in repo.Refs.FromGlob(prefix + "*"))
            {
                string localCanonicalName = "refs/heads/" + remoteTrackingReference.CanonicalName.Substring(prefix.Length);

                repo.Refs.Add(localCanonicalName, new ObjectId(remoteTrackingReference.TargetIdentifier), true);
            }
        }

        private static void EnsureOnlyOneRemoteIsDefined(IRepository repo)
        {
            var howMany = repo.Network.Remotes.Count();

            if (howMany == 1)
            {
                return;
            }

            throw new ErrorException(string.Format(
                "{0} remote(s) have been detected. "
                + "When being run on a TeamCity agent, the Git repository is "
                + "expected to bear one (and no more than one) remote.", howMany));
        }
    }
}
