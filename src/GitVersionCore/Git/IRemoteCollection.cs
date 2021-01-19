using System.Collections.Generic;

namespace GitVersion
{
    public interface IRemoteCollection : IEnumerable<IRemote>
    {
        IRemote this[string name] { get; }
    }
}
