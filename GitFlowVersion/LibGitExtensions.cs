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
        
        public static SemanticVersion SemVerTags(this IRepository repository, Commit commit)
        {
            var semVerTags = repository.Tags.Where(tag => tag.Target == commit)
                                               .Where(tag => SemanticVersionParser.IsVersion(tag.Name)).ToList();
            if (semVerTags.Count > 1)
            {
                throw new Exception(string.Format("Error processing commit `{0}`. Only one version version tag per commit is allowed", commit.Sha));
            }
            var first = semVerTags.FirstOrDefault();
            if (first != null)
            {
                return SemanticVersionParser.FromMajorMinorPatch(first.Name);
            }
            return null;
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

        public static DateTimeOffset CommitTimeStamp(this Tag tag)
        {
            var commit = tag.Target as Commit;
            if (commit == null)
            {
                throw new InvalidOperationException();
            }
            return commit.When();
        }

        public static bool IsBefore(this Tag tag, Commit commit)
        {
            var tagCommit = tag.Target as Commit;
            if (tagCommit == null)
            {
                throw new InvalidOperationException();
            }
            return tagCommit.When() <= commit.When();
        }

        public static Branch GetBranch(this IRepository repository, string name)
        {
            var branch = repository.Branches.FirstOrDefault(b => b.Name == name);

            if (branch == null)
            {
               
                if (!repository.Network.Remotes.Any())
                {
                    Logger.Write("No remotes found");
                }
                else
                {
                    var remote = repository.Network.Remotes.First();

                    Logger.Write(string.Format("No local branch with name {0} found, going to try on the remote {1}({2})", name, remote.Name, remote.Url));
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