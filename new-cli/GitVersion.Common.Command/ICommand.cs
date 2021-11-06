using System.Threading.Tasks;

namespace GitVersion.Command;

public interface ICommand
{
    Task<int> InvokeAsync(object command);
}