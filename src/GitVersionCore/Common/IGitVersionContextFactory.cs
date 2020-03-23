using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        GitVersionContext Create(Arguments arguments, IRepository repository);
    }
}
