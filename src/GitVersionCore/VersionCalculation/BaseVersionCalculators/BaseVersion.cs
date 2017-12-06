using GitVersion.GitRepoInformation;
using System;

namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class BaseVersion
    {
        GitVersionContext _context;

        public BaseVersion(
            GitVersionContext context, bool shouldIncrement,
            SemanticVersion semanticVersion, BaseVersionSource source,
            string branchNameOverride)
        {
            ShouldIncrement = shouldIncrement;
            SemanticVersion = semanticVersion;
            Source = source;
            BranchNameOverride = branchNameOverride;
            _context = context;
        }

        public bool ShouldIncrement { get; }

        public SemanticVersion SemanticVersion { get; }

        public BaseVersionSource Source { get; }

        public string BranchNameOverride { get; }

        public override string ToString()
        {
            return $"{Source.Description}: {SemanticVersion.ToString("f")} with commit count of {Source.Commit.DistanceFromTip} (Incremented: {(ShouldIncrement ? BaseVersionCalculator.MaybeIncrement(_context, this).ToString("t") : "None")})";
        }
    }

    public class BaseVersionSource
    {
        public BaseVersionSource(MCommit sourceCommit, string description)
        {
            Description = description;
            Commit = sourceCommit;
        }

        public string Description { get; }
        public MCommit Commit { get; }
    }
}