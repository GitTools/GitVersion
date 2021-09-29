using System;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion.VersionCalculation
{
    public class PathFilter : IVersionFilter
    {
        private readonly static Dictionary<string, IEnumerable<string>> patchsCache = new Dictionary<string, IEnumerable<string>>();

        public enum PathFilterMode { Inclusive, Exclusive }

        private readonly IEnumerable<string> paths;
        private readonly PathFilterMode mode;
        private readonly IGitRepository repo;

        public PathFilter(IEnumerable<string> paths, PathFilterMode mode = PathFilterMode.Inclusive)
        {
            this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
            this.mode = mode;
        }

        public bool Exclude(BaseVersion version, out string reason)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            reason = null;
            if (version.Source.StartsWith("Fallback") || version.Source.StartsWith("Git tag") || version.Source.StartsWith("NextVersion")) return false;

            return Exclude(version.BaseVersionSource, version.Context, out reason);
        }

        public bool Exclude(ICommit commit, GitVersionContext context, out string reason)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));

            reason = null;

            var match = new System.Text.RegularExpressions.Regex($"^({context.Configuration.GitTagPrefix}).*$", System.Text.RegularExpressions.RegexOptions.Compiled);

            

            IEnumerable<string> patch = null;
            if (!patchsCache.ContainsKey(commit.Sha))
            {
                //if (!context.Repository.Tags.Any(t => t.Target.Sha == commit.Sha && match.IsMatch(t.FriendlyName)))
                //{
                //    Tree commitTree = commit.Tree; // Main Tree
                //    Tree parentCommitTree = commit.Parents.FirstOrDefault()?.Tree; // Secondary Tree
                //    patch = context.Repository.Diff.Compare<Patch>(parentCommitTree, commitTree); // Difference
                //}
                //patchsCache[commit.Sha] = patch;

                if (!this.repo.Tags.Any(t => t.TargetSha == commit.Sha && match.IsMatch(t.Name.Friendly)))

                    patch = this.repo.DiffPathChanges(commit.Parents.FirstOrDefault(), commit);
            }
            patchsCache[commit.Sha] = patch;


            patch = patchsCache[commit.Sha];
            if (patch != null)
            {
                switch (mode)
                {
                    case PathFilterMode.Inclusive:
                        if (!paths.Any(path => patch.Any(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
                        {
                            reason = "Source was ignored due to commit path is not present";
                            return true;
                        }
                        break;
                    case PathFilterMode.Exclusive:
                        if (paths.Any(path => patch.All(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
                        {
                            reason = "Source was ignored due to commit path excluded";
                            return true;
                        }
                        break;
                }
            }


            return false;
        }
    }
}
