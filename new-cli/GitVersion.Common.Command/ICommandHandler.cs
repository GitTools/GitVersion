using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitVersion.Command
{
    public interface ICommandHandler
    {
        Task<int> InvokeAsync(GitVersionOptions options);

        IEnumerable<ICommandHandler> SubCommands();
    }
}