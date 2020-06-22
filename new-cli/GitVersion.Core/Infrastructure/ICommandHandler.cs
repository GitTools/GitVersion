using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitVersion.Core.Infrastructure
{
    public interface ICommandHandler
    {
        Task<int> InvokeAsync(GitVersionOptions options);

        IEnumerable<ICommandHandler> SubCommands();
    }
}