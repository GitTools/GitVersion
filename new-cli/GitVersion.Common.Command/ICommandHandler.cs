using System.Threading.Tasks;

namespace GitVersion.Command
{
    public interface ICommandHandler
    {
        Task<int> InvokeAsync(object command);
    }
}