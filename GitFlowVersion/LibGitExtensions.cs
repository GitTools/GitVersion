namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using LibGit2Sharp;

    public static class LibGitExtensions
    {
        static FieldInfo commitRepoField;
        static FieldInfo branchRepoField;

        static LibGitExtensions()
        {
            branchRepoField = typeof(Branch).GetField("repo", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            commitRepoField = typeof(Commit).GetField("repo", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
        }

        public static Repository Repository(this Branch branch)
        {
            return (Repository)branchRepoField.GetValue(branch);
        }

        public static Repository Repository(this Commit commit)
        {
            return (Repository)commitRepoField.GetValue(commit);
        }

        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }
        
        public static SemanticVersion SemVerTag(this IRepository repository, Commit commit)
        {
            var semVerTags = repository.SemVerTags(commit).ToList();
            if (semVerTags.Count > 1)
            {
                throw new ErrorException(string.Format("Error processing commit `{0}`. Only one version version tag per commit is allowed.", commit.Sha));
            }
            return semVerTags.FirstOrDefault();
        }
       public static string Prefix(this ObjectId objectId)
        {
            return objectId.Sha.Substring(0, 8);
        }
       public static string Prefix(this Commit commit)
        {
            return commit.Sha.Substring(0, 8);
        }

        public static IEnumerable<SemanticVersion> SemVerTags(this IRepository repository, Commit commit)
        {
            foreach (var tag in repository.Tags.Where(tag => tag.Target == commit))
            {
                SemanticVersion version;
                if (SemanticVersionParser.TryParse(tag.Name, out version))
                {
                    yield return version;
                }
            }
        }

        public static Reference ToReference(this Branch branch)
        {
            return branch.Repository().Refs.First(x => x.CanonicalName == branch.CanonicalName);
        }

        public static bool IsOnBranch(this Commit commit, Branch branch)
        {
            return branch.Repository().Refs.ReachableFrom(new[] { branch.ToReference() }, new[] { commit }).Any();
        }

        public static bool IsOnBranch(this IRepository repository, Branch branch, Commit commit)
        {
            return repository.Refs.ReachableFrom(new[] { branch.ToReference() }, new[] { commit }).Any();
        }

        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static bool IsMinorLargerThan(this SemanticVersion version, SemanticVersion o)
        {
            if (version.Major > o.Major)
            {
                return true;
            }
            if ((version.Major == o.Major) && (version.Minor > o.Minor))
            {
                return true;
            }
            return false;
        }
        
        public static bool IsOnBranch(this Tag tag, Branch branch)
        {
            var commit = tag.Target as Commit;
            if (commit == null)
            {
                return false;
            }
            return commit.IsOnBranch(branch);
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

        public static Branch DevelopBranch(this IRepository repository)
        {
            return repository.GetBranch("develop");
        }

        public static Branch MasterBranch(this IRepository repository)
        {
            return repository.GetBranch("master");
        }
    }
}