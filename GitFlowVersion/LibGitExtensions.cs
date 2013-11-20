namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public static class LibGitExtensions
    {

        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }

        public static string Prefix(this ObjectId objectId)
        {
            return objectId.Sha.Substring(0, 8);
        }
        public static string Prefix(this Commit commit)
        {
            return commit.Sha.Substring(0, 8);
        }

        public static SemanticVersion NewestSemVerTag(this IRepository repository, Commit commit)
        {
            foreach (var tag in repository.Tags.Reverse().Where(tag => tag.Target == commit))
            {
                SemanticVersion version;
                if (SemanticVersionParser.TryParse(tag.Name, out version))
                {
                    return version;
                }
            }
            return null;
        }

        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static Branch GetBranch(this IRepository repository, string name)
        {
            var branch = repository.Branches.FirstOrDefault(b => b.Name == name);

            if (branch == null)
            {

                if (!repository.Network.Remotes.Any())
                {
                    Logger.WriteInfo("No remotes found");
                }
                else
                {
                    var remote = repository.Network.Remotes.First();

                    Logger.WriteInfo(string.Format("No local branch with name {0} found, going to try on the remote {1}({2})", name, remote.Name, remote.Url));
                    try
                    {
                        repository.Network.Fetch(remote);
                    }
                    catch (LibGit2SharpException exception)
                    {
                        if (exception.Message.Contains("This transport isn't implemented"))
                        {
                            var message = string.Format("Could not fetch from '{0}' since LibGit2 does not support the transport. You have most likely cloned using SSH. If there is a remote branch named '{1}' then fetch it manually, otherwise please create a local branch named '{1}'.", remote.Url, name);
                            throw new MissingBranchException(message, exception);
                        }
                        throw;
                    }

                    branch = repository.Branches.FirstOrDefault(b => b.Name.EndsWith("/" + name));
                }
            }

            if (branch == null)
            {
                var branchNames = string.Join(";", repository.Branches);
                var message = string.Format("Could not find branch '{0}' in the repository, please create one. Existing branches:{1}", name, branchNames);
                throw new Exception(message);
            }

            return branch;
        }


    }
}
