using System;
using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        IGitRepositoryCommands Commands { get; }
        ObjectDatabase ObjectDatabase { get; }
        Branch Head { get; }
        CommitCollection Commits { get; }
        BranchCollection Branches { get; }
        TagCollection Tags { get; }
        ReferenceCollection Refs { get; }
        Diff Diff { get; }
        RepositoryInformation Info { get; }
        Network Network { get; }
        RepositoryStatus RetrieveStatus();
    }
}
