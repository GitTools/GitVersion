using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitRepository : IRepository
    {
        IGitRepositoryCommands Commands { get; }
    }
}
