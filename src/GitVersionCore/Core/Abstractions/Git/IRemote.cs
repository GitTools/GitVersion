using System;

namespace GitVersion
{
    public interface IRemote : IEquatable<IRemote>, IComparable<IRemote>
    {
        string Name { get; }
        string RefSpecs { get; }
    }
}
