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

        public static IEnumerable<Commit> SpecificCommits(this Branch branch)
        {
            var firstCommitOnBranch = branch.Repository()
                .Refs
                .Log(branch.CanonicalName)
                .Last();
            foreach (var commit in branch.Commits)
            {
                if (commit.Id == firstCommitOnBranch.To)
                {
                    yield return commit;
                    break;
                }
                yield return commit;
            }
        }




        public static Repository Repository(this Commit commit)
        {
            return (Repository)commitRepoField.GetValue(commit);
        }
        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }
        public static IEnumerable<Tag> CommitTags(this Repository repository)
        {
            return repository.Tags.Where(tag => tag.Target is Commit);
        }
        public static IEnumerable<Tag> Tags(this Commit commit)
        {
            return commit.Repository().Tags.Where(tag => tag.Target == commit);
        }
        public static IEnumerable<Tag> SemVerTags(this Commit commit)
        {
            return commit.Tags().Where(tag => SemanticVersion.IsVersion(tag.Name));
        }
        public static IEnumerable<Reference> LocalBranchRefs(this Repository repository)
        {
            return repository.Refs.Where(r => r.IsLocalBranch());
        }
        public static IEnumerable<Reference> TagRefs(this Repository repository)
        {
            return repository.Refs.Where(r => r.IsTag());
        }
        public static Reference ToReference(this Branch branch)
        {
            return branch.Repository().Refs.First(x => x.CanonicalName == branch.CanonicalName);
        }
        public static bool IsOnBranch(this Commit commit, Branch branch)
        {
            return branch.Repository().Refs.ReachableFrom(new[] { branch.ToReference() }, new[] { commit }).Any();
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

        public static Branch GetBranch(this Repository repository, string name)
        {
            var branch = repository.Branches.FirstOrDefault(b => b.Name == name);

            if (branch == null)
            {
                throw new Exception(string.Format("Could not find branch {0} in the repository, please create one. Existing branches:{1}", name, string.Join(";", repository.Branches)));
            }

            return branch;
        }

        public static Branch GetDevelopBranch(this Repository repository)
        {
            return repository.GetBranch("develop");
        }

        public static Branch GetMasterBranch(this Repository repository)
        {
            return repository.GetBranch("master");
        }
    }
}